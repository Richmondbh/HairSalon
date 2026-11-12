namespace HairSalon.Client.Services;

/// <summary>
/// Thrown by the API clients when a request fails. Message is always a text that can be
/// shown to the user, e.g. "Sorry, that time has just been booked."
/// </summary>
public class ApiException(string message, int statusCode = 0) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
