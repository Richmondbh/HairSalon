using System.Security.Claims;
using HairSalon.Shared.Rules;

namespace HairSalon.Api.Infrastructure;

/// <summary>
/// EXTENSION METHODS on ClaimsPrincipal (the "User" property in every controller).
/// They read values from the JWT claims that TokenService put into the token.
/// Usage in a controller:  var userId = User.GetUserId();
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>The user's id from the "sub" (subject) claim.</summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value
        ?? throw new ForbiddenException("Could not identify the logged-in user.");

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
