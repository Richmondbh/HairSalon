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
}
