using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// REVIEWS AND RATINGS 
/// Rules: you can only review your OWN, COMPLETED appointment, and only once.
/// </summary>
public class ReviewService(SalonDbContext db, NotificationService notifications)
{
    public async Task<ReviewDto> CreateAsync(string customerId, CreateReviewDto dto)
    {
        var appointment = await db.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Stylist)
            .Include(a => a.Service)
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId)
            ?? throw new NotFoundException("The appointment was not found.");

        if (appointment.CustomerId != customerId)
            throw new ForbiddenException("You can only review your own appointments.");

        if (appointment.Status != AppointmentStatus.Completed)
            throw new BusinessRuleException("You can review an appointment after the visit has been completed.");

        if (appointment.Review != null)
            throw new BusinessRuleException("You have already reviewed this appointment.");

        var review = new Review
        {
            AppointmentId = appointment.AppointmentId,
            Appointment = appointment,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            CreatedAt = DateTime.Now
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        await notifications.NotifyAdminsAsync("New review",
            $"{appointment.Customer.FullName} gave {appointment.Stylist.FullName} {dto.Rating} star(s) for {appointment.Service.Name}.",
            NotificationType.Review, appointment.AppointmentId);

        return review.ToDto();
    }

    /// <summary>All reviews for one stylist, newest first.</summary>
    public async Task<List<ReviewDto>> GetForStylistAsync(int stylistId)
    {
        var reviews = await ReviewsWithDetails()
            .Where(r => r.Appointment.StylistId == stylistId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return reviews.Select(r => r.ToDto()).ToList();
    }

    /// <summary>The newest good reviews (4-5 stars) with a comment - for the home page.</summary>
    public async Task<List<ReviewDto>> GetLatestAsync(int count)
    {
        var reviews = await ReviewsWithDetails()
            .Where(r => r.Rating >= 4 && r.Comment != null)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
        return reviews.Select(r => r.ToDto()).ToList();
    }

    /// <summary>ThenInclude loads a navigation property of a navigation property.</summary>
    private IQueryable<Review> ReviewsWithDetails() =>
        db.Reviews
            .AsNoTracking()
            .Include(r => r.Appointment).ThenInclude(a => a.Customer)
            .Include(r => r.Appointment).ThenInclude(a => a.Stylist)
            .Include(r => r.Appointment).ThenInclude(a => a.Service);
}
