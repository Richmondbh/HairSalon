using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;

namespace HairSalon.Api.Mapping;

/// <summary>
/// Converts ENTITIES (database classes) into DTOs (what the API sends).
/// Written by hand instead of using a library like AutoMapper, so you can see exactly
/// which property goes where. Usage:  var dto = service.ToDto();
/// </summary>
public static class MappingExtensions
{
    public static ServiceDto ToDto(this Service s) => new()
    {
        ServiceId = s.ServiceId,
        Name = s.Name,
        Description = s.Description,
        Price = s.Price,
        DurationMinutes = s.DurationMinutes,
        Category = s.Category,
        IsActive = s.IsActive
    };

    /// <summary>Copies the values from an edit form onto an entity (create and update).</summary>
    public static void UpdateFrom(this Service s, ServiceEditDto dto)
    {
        s.Name = dto.Name.Trim();
        s.Description = dto.Description?.Trim() ?? string.Empty;
        s.Price = dto.Price;
        s.DurationMinutes = dto.DurationMinutes;
        s.Category = dto.Category;
        s.IsActive = dto.IsActive;
    }

    public static StylistDto ToDto(this Stylist s) => new()
    {
        StylistId = s.StylistId,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        Specialization = s.Specialization,
        YearsOfExperience = s.YearsOfExperience,
        IsAvailable = s.IsAvailable,
        Bio = s.Bio
    };

    public static void UpdateFrom(this Stylist s, StylistEditDto dto)
    {
        s.FirstName = dto.FirstName.Trim();
        s.LastName = dto.LastName.Trim();
        s.Email = dto.Email.Trim();
        s.PhoneNumber = dto.PhoneNumber.Trim();
        s.Specialization = dto.Specialization.Trim();
        s.YearsOfExperience = dto.YearsOfExperience;
        s.IsAvailable = dto.IsAvailable;
        s.Bio = dto.Bio?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Needs Service, Stylist and Customer to be loaded with .Include(...) first,
    /// otherwise the navigation properties are null.
    /// </summary>
    public static AppointmentDto ToDto(this Appointment a) => new()
    {
        AppointmentId = a.AppointmentId,
        AppointmentDate = a.AppointmentDate,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Status = a.Status,
        Notes = a.Notes,
        Price = a.Price,
        CreatedAt = a.CreatedAt,
        ServiceId = a.ServiceId,
        ServiceName = a.Service?.Name ?? string.Empty,
        StylistId = a.StylistId,
        StylistName = a.Stylist?.FullName ?? string.Empty,
        CustomerId = a.CustomerId,
        CustomerName = a.Customer?.FullName ?? string.Empty,
        CustomerEmail = a.Customer?.Email ?? string.Empty,
        HasReview = a.Review != null,
    };

    public static NotificationDto ToDto(this Notification n) => new()
    {
        NotificationId = n.NotificationId,
        Title = n.Title,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        AppointmentId = n.AppointmentId
    };

    /// <summary>Needs Appointment (+ its Customer, Stylist and Service) to be included.</summary>
    public static ReviewDto ToDto(this Review r) => new()
    {
        ReviewId = r.ReviewId,
        AppointmentId = r.AppointmentId,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt,
        CustomerName = ShortName(r.Appointment?.Customer),
        StylistId = r.Appointment?.StylistId ?? 0,
        StylistName = r.Appointment?.Stylist?.FullName ?? string.Empty,
        ServiceName = r.Appointment?.Service?.Name ?? string.Empty
    };

    /// <summary>"Anna Svensson" -> "Anna S." (privacy on public pages).</summary>
    private static string ShortName(ApplicationUser? user) =>
        user == null ? "Customer"
        : string.IsNullOrEmpty(user.LastName) ? user.FirstName
        : $"{user.FirstName} {user.LastName[0]}.";
}
