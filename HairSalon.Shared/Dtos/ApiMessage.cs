namespace HairSalon.Shared.Dtos;

/// <summary>
/// A simple { "message": "..." } object. The API uses it for error messages
/// (400/403/404) and for short confirmations such as "Service deleted".
/// </summary>
public class ApiMessage
{
    public string Message { get; set; } = string.Empty;

    public ApiMessage() { }
    public ApiMessage(string message) => Message = message;
}
