using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Diagnostics;

namespace HairSalon.Api.Infrastructure;

/// <summary>
/// GLOBAL ERROR HANDLING. Registered in Program.cs with AddExceptionHandler and
/// app.UseExceptionHandler(). Any exception that escapes a controller ends up here.
///
/// Known (business) exceptions become a 400/403/404 with a friendly message, which the
/// Blazor client shows to the user. Unknown exceptions are logged and become a generic 500,
/// so internal details (stack traces, SQL) are never sent to the browser.
/// </summary>
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            BusinessRuleException => (StatusCodes.Status400BadRequest, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiMessage(message), cancellationToken);

        return true;   // true = "handled, don't pass it on"
    }
}
