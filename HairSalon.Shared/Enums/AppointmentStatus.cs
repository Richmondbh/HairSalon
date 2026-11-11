namespace HairSalon.Shared.Enums;

/// <summary>
/// The life cycle of an appointment.
/// Pending   -> the customer has booked, the salon has not approved it yet
/// Confirmed -> approved by an administrator
/// Completed -> the visit has taken place (counts as revenue and loyalty points)
/// Cancelled -> cancelled by the customer or the salon (the time slot becomes free again)
/// </summary>
public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}
