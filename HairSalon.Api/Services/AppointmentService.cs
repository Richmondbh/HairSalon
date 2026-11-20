using System.Globalization;
using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// BUSINESS LOGIC for bookings - the heart of the application.
/// All the rules live here (opening hours, no double bookings, who may cancel what,
/// which status changes are allowed), so they are enforced no matter which client calls the API.
/// </summary>
public class AppointmentService(
    SalonDbContext db,
    NotificationService notifications,
    LoyaltyService loyalty)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// APPOINTMENT STATUS MANAGEMENT: which status may follow which.
    ///   Pending   -> Confirmed or Cancelled
    ///   Confirmed -> Completed or Cancelled
    ///   Completed and Cancelled are final.
    /// </summary>
    private static readonly Dictionary<AppointmentStatus, AppointmentStatus[]> AllowedTransitions = new()
    {
        [AppointmentStatus.Pending] = [AppointmentStatus.Confirmed, AppointmentStatus.Cancelled],
        [AppointmentStatus.Confirmed] = [AppointmentStatus.Completed, AppointmentStatus.Cancelled],
        [AppointmentStatus.Completed] = [],
        [AppointmentStatus.Cancelled] = []
    };

    // =============================== Time slots ===============================

    /// <summary>
    /// Calculates the free start times for a stylist on a date for a given service.
    /// Example: a 60-minute service, opening hours 09-18 and a booking 10:00-11:00
    /// gives 09:00, 11:00, 11:30 ... 17:00 (09:30 and 10:00/10:30 would overlap).
    /// </summary>
    public async Task<List<TimeSlotDto>> GetAvailableSlotsAsync(int stylistId, int serviceId, DateOnly date, int? excludeAppointmentId = null)
    {
        var service = await db.Services.FindAsync(serviceId)
                      ?? throw new NotFoundException("The service was not found.");
        var stylist = await db.Stylists.FindAsync(stylistId)
                      ?? throw new NotFoundException("The stylist was not found.");

        if (!stylist.IsAvailable || !SalonHours.IsOpen(date) || date < Today)
            return [];

        var booked = await GetBookedTimesAsync(stylistId, date, excludeAppointmentId);
        var now = DateTime.Now;
        var openMinute = ToMinutes(SalonHours.OpeningTime);
        var closeMinute = ToMinutes(SalonHours.ClosingTime);
        var possibleStarts = (closeMinute - openMinute) / SalonHours.SlotIntervalMinutes + 1;

        // A LINQ pipeline: every possible start time -> keep the ones that fit before closing
        // time, are still in the future and do not overlap an existing booking.
        return Enumerable.Range(0, possibleStarts)
            .Select(i => openMinute + i * SalonHours.SlotIntervalMinutes)
            .Where(start => start + service.DurationMinutes <= closeMinute)
            .Select(start => new TimeSlotDto
            {
                StartTime = FromMinutes(start),
                EndTime = FromMinutes(start + service.DurationMinutes)
            })
            .Where(slot => date.ToDateTime(slot.StartTime) > now)
            .Where(slot => !booked.Any(b => Overlaps(slot.StartTime, slot.EndTime, b.Start, b.End)))
            .ToList();
    }

    // =============================== Customer actions ===============================

    public async Task<AppointmentDto> CreateAsync(string customerId, CreateAppointmentDto dto)
    {
        var service = await db.Services.FindAsync(dto.ServiceId)
                      ?? throw new NotFoundException("The service was not found.");
        if (!service.IsActive)
            throw new BusinessRuleException("This service is no longer offered.");

        var stylist = await db.Stylists.FindAsync(dto.StylistId)
                      ?? throw new NotFoundException("The stylist was not found.");
        if (!stylist.IsAvailable)
            throw new BusinessRuleException($"{stylist.FullName} is not taking bookings at the moment.");

        var start = dto.StartTime ?? throw new BusinessRuleException("Please choose a time.");
        await ValidateTimeAsync(dto.AppointmentDate, start, service.DurationMinutes, stylist.StylistId, customerId, excludeAppointmentId: null);

        var appointment = new Appointment
        {
            AppointmentDate = dto.AppointmentDate,
            StartTime = start,
            EndTime = start.AddMinutes(service.DurationMinutes),
            Status = AppointmentStatus.Pending,      // an admin must confirm it
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Price = service.Price,
            CustomerId = customerId,
            ServiceId = service.ServiceId,
            StylistId = stylist.StylistId,
            CreatedAt = DateTime.Now
        };

        // LOYALTY: Silver and Gold customers get a discount on new bookings.
        appointment.DiscountPercent = await loyalty.GetDiscountPercentAsync(customerId);
        appointment.Price = Math.Round(service.Price * (100 - appointment.DiscountPercent) / 100m, 0);

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var created = await LoadAsync(appointment.AppointmentId);

        await notifications.NotifyUserAsync(customerId, "Booking received",
            $"We have received your booking: {Describe(created)}. You will be notified when the salon confirms it.",
            NotificationType.Booking, created.AppointmentId);
        await notifications.NotifyAdminsAsync("New booking",
            $"{created.Customer.FullName} booked {Describe(created)}. Please confirm it.",
            NotificationType.Booking, created.AppointmentId);

        return created.ToDto();
    }

    /// <summary>
    /// The customer's own appointments. filter = "upcoming", "history" or null (all).
    /// Users can only ever see their own bookings because the id comes from the JWT,
    /// not from the URL.
    /// </summary>
    public async Task<List<AppointmentDto>> GetForCustomerAsync(string customerId, string? filter)
    {
        var appointments = await WithDetails()
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();

        var dtos = appointments.Select(a => a.ToDto());

        // IsUpcoming is defined once in the Shared project and reused here.
        return filter?.ToLower() switch
        {
            "upcoming" => dtos.Where(a => a.IsUpcoming).OrderBy(a => a.StartDateTime).ToList(),
            "history" => dtos.Where(a => !a.IsUpcoming).OrderByDescending(a => a.StartDateTime).ToList(),
            _ => dtos.OrderByDescending(a => a.StartDateTime).ToList()
        };
    }

    public async Task<AppointmentDto> GetByIdAsync(int id, string userId, bool isAdmin)
    {
        var appointment = await LoadAsync(id);
        if (!isAdmin && appointment.CustomerId != userId)
            throw new ForbiddenException("You can only view your own appointments.");
        return appointment.ToDto();
    }

    /// <summary>Customers can cancel their own upcoming appointments; admins can cancel any.</summary>
    public async Task<AppointmentDto> CancelAsync(int id, string userId, bool isAdmin)
    {
        var appointment = await LoadAsync(id);

        if (!isAdmin && appointment.CustomerId != userId)
            throw new ForbiddenException("You can only cancel your own appointments.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new BusinessRuleException($"This appointment is already {appointment.Status.ToString().ToLower()}.");

        if (!isAdmin && appointment.AppointmentDate.ToDateTime(appointment.StartTime) <= DateTime.Now)
            throw new BusinessRuleException("An appointment that has already started cannot be cancelled.");

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();

        if (isAdmin)
            await notifications.NotifyUserAsync(appointment.CustomerId, "Appointment cancelled",
                $"The salon has cancelled your {Describe(appointment)}. Please contact us or book a new time.",
                NotificationType.StatusChange, appointment.AppointmentId);
        else
            await notifications.NotifyAdminsAsync("Booking cancelled",
                $"{appointment.Customer.FullName} cancelled {Describe(appointment)}. The time is free again.",
                NotificationType.StatusChange, appointment.AppointmentId);

        return appointment.ToDto();
    }

    /// <summary>
    /// Moves an appointment to another date/time with the same stylist.
    /// If a CUSTOMER moves it, it must be confirmed again (status -> Pending).
    /// If an ADMIN moves it, it is confirmed directly.
    /// </summary>
    public async Task<AppointmentDto> RescheduleAsync(int id, string userId, bool isAdmin, RescheduleDto dto)
    {
        var appointment = await LoadAsync(id);

        if (!isAdmin && appointment.CustomerId != userId)
            throw new ForbiddenException("You can only change your own appointments.");

        if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new BusinessRuleException("Only pending or confirmed appointments can be moved.");

        if (!appointment.Stylist.IsAvailable)
            throw new BusinessRuleException($"{appointment.Stylist.FullName} is not taking bookings at the moment.");

        var start = dto.NewStartTime ?? throw new BusinessRuleException("Please choose a time.");
        var duration = (int)(appointment.EndTime - appointment.StartTime).TotalMinutes;

        await ValidateTimeAsync(dto.NewDate, start, duration, appointment.StylistId, appointment.CustomerId, appointment.AppointmentId);

        var oldDescription = Describe(appointment);
        appointment.AppointmentDate = dto.NewDate;
        appointment.StartTime = start;
        appointment.EndTime = start.AddMinutes(duration);
        appointment.Status = isAdmin ? AppointmentStatus.Confirmed : AppointmentStatus.Pending;
        appointment.ReminderSent = false;   // the new time needs a new reminder
        await db.SaveChangesAsync();

        if (isAdmin)
            await notifications.NotifyUserAsync(appointment.CustomerId, "Appointment moved",
                $"The salon moved your appointment. New time: {Describe(appointment)}.",
                NotificationType.StatusChange, appointment.AppointmentId);
        else
            await notifications.NotifyAdminsAsync("Booking moved",
                $"{appointment.Customer.FullName} moved {oldDescription} to {Describe(appointment)}. Please confirm it.",
                NotificationType.Booking, appointment.AppointmentId);

        return appointment.ToDto();
    }

    // =============================== Admin actions ===============================

    /// <summary>All appointments with optional filters (admin "Appointments" page).</summary>
    public async Task<List<AppointmentDto>> GetAllAsync(
        AppointmentStatus? status, int? stylistId, DateOnly? from, DateOnly? to, string? search)
    {
        var query = WithDetails().AsNoTracking();

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (stylistId.HasValue) query = query.Where(a => a.StylistId == stylistId.Value);
        if (from.HasValue) query = query.Where(a => a.AppointmentDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.AppointmentDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Customer.FirstName.ToLower().Contains(term) ||
                a.Customer.LastName.ToLower().Contains(term) ||
                (a.Customer.Email ?? "").ToLower().Contains(term) ||
                a.Service.Name.ToLower().Contains(term));
        }

        var appointments = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

        return appointments.Select(a => a.ToDto()).ToList();
    }

    /// <summary>Approve (Confirmed), finish (Completed) or cancel an appointment.</summary>
    public async Task<AppointmentDto> UpdateStatusAsync(int id, UpdateStatusDto dto)
    {
        var appointment = await LoadAsync(id);

        if (appointment.Status == dto.Status)
            return appointment.ToDto();

        if (!AllowedTransitions[appointment.Status].Contains(dto.Status))
            throw new BusinessRuleException($"An appointment that is {appointment.Status} cannot be changed to {dto.Status}.");

        if (dto.Status == AppointmentStatus.Completed &&
            appointment.AppointmentDate.ToDateTime(appointment.StartTime) > DateTime.Now)
            throw new BusinessRuleException("An appointment cannot be completed before it has started.");

        appointment.Status = dto.Status;
        await db.SaveChangesAsync();

        var (title, text) = dto.Status switch
        {
            AppointmentStatus.Confirmed => ("Booking confirmed",
                $"Your {Describe(appointment)} is confirmed. See you then!"),
            AppointmentStatus.Completed => ("Thank you for your visit!",
                $"We hope you enjoyed your {appointment.Service.Name}. You can now leave a review under 'My appointments'."),
            AppointmentStatus.Cancelled => ("Appointment cancelled",
                $"The salon has cancelled your {Describe(appointment)}."),
            _ => ("Appointment updated", $"Your {Describe(appointment)} is now {dto.Status}.")
        };

        if (!string.IsNullOrWhiteSpace(dto.Message))
            text += $" Message from the salon: {dto.Message.Trim()}";

        if (dto.Status == AppointmentStatus.Completed)
        {
            var summary = await loyalty.GetSummaryAsync(appointment.CustomerId);
            text += $" You earned {LoyaltyRules.CalculatePoints(appointment.Price)} loyalty points and now have {summary.Points} ({summary.Tier}).";
        }

        await notifications.NotifyUserAsync(appointment.CustomerId, title, text,
            NotificationType.StatusChange, appointment.AppointmentId);

        return appointment.ToDto();
    }

    // =============================== Helpers ===============================

    /// <summary>Checks every rule for a date/time. Throws a BusinessRuleException if one is broken.</summary>
    private async Task ValidateTimeAsync(DateOnly date, TimeOnly start, int durationMinutes,
        int stylistId, string customerId, int? excludeAppointmentId)
    {
        if (!SalonHours.IsOpen(date))
            throw new BusinessRuleException("The salon is closed on Sundays.");

        if (date > Today.AddDays(SalonHours.MaxDaysAhead))
            throw new BusinessRuleException($"Bookings can be made at most {SalonHours.MaxDaysAhead} days ahead.");

        if (date.ToDateTime(start) <= DateTime.Now)
            throw new BusinessRuleException("You cannot book a time that has already passed.");

        var startMinute = ToMinutes(start);
        var openMinute = ToMinutes(SalonHours.OpeningTime);
        if (startMinute < openMinute || startMinute + durationMinutes > ToMinutes(SalonHours.ClosingTime))
            throw new BusinessRuleException(
                $"The appointment must fit within our opening hours ({SalonHours.OpeningTime:HH\\:mm}-{SalonHours.ClosingTime:HH\\:mm}).");

        if ((startMinute - openMinute) % SalonHours.SlotIntervalMinutes != 0)
            throw new BusinessRuleException($"Appointments start every {SalonHours.SlotIntervalMinutes} minutes.");

        var end = start.AddMinutes(durationMinutes);

        var stylistBookings = await GetBookedTimesAsync(stylistId, date, excludeAppointmentId);
        if (stylistBookings.Any(b => Overlaps(start, end, b.Start, b.End)))
            throw new BusinessRuleException("Sorry, that time has just been booked. Please choose another time.");

        var customerBookings = await db.Appointments
            .Where(a => a.CustomerId == customerId && a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled && a.AppointmentId != excludeAppointmentId)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();
        if (customerBookings.Any(b => Overlaps(start, end, b.StartTime, b.EndTime)))
            throw new BusinessRuleException("You already have another appointment at that time.");
    }

    /// <summary>The stylist's booked (not cancelled) times on a date.</summary>
    private async Task<List<(TimeOnly Start, TimeOnly End)>> GetBookedTimesAsync(int stylistId, DateOnly date, int? excludeAppointmentId)
    {
        var rows = await db.Appointments
            .Where(a => a.StylistId == stylistId && a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled && a.AppointmentId != excludeAppointmentId)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        return rows.Select(r => (r.StartTime, r.EndTime)).ToList();
    }

    /// <summary>Two time ranges overlap when each one starts before the other one ends.</summary>
    private static bool Overlaps(TimeOnly start, TimeOnly end, TimeOnly otherStart, TimeOnly otherEnd) =>
        start < otherEnd && otherStart < end;

    private static int ToMinutes(TimeOnly time) => time.Hour * 60 + time.Minute;

    private static TimeOnly FromMinutes(int minutes) => new(minutes / 60, minutes % 60);

    /// <summary>Loads an appointment WITH its service, stylist and customer (eager loading).</summary>
    private async Task<Appointment> LoadAsync(int id) =>
        await WithDetails().FirstOrDefaultAsync(a => a.AppointmentId == id)
        ?? throw new NotFoundException($"Appointment {id} was not found.");

    /// <summary>
    /// .Include(...) = EAGER LOADING: EF Core joins the related tables in the same SQL query,
    /// so a.Service.Name etc. are available without extra database calls.
    /// </summary>
    private IQueryable<Appointment> WithDetails()
    {
        IQueryable<Appointment> query = db.Appointments
            .Include(a => a.Service)
            .Include(a => a.Stylist)
            .Include(a => a.Customer);
        query = query.Include(a => a.Review);
        return query;
    }

    /// <summary>"Women's Haircut with Emma Lindqvist on Thu 12 Nov at 10:00"</summary>
    private static string Describe(Appointment a) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{a.Service.Name} with {a.Stylist.FullName} on {a.AppointmentDate:ddd d MMM} at {a.StartTime:HH\\:mm}");
}
