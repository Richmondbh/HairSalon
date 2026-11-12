using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using Microsoft.IdentityModel.Tokens;

namespace HairSalon.Api.Services;

/// <summary>
/// Creates JSON Web Tokens (JWT).
///
/// A JWT has three parts: header.payload.signature
///  - the PAYLOAD holds "claims" (facts about the user: id, name, roles, expiry)
///  - the SIGNATURE is made with the secret key in appsettings.json. If anyone changes the
///    payload (e.g. adds role=Admin) the signature no longer matches and the API rejects it.
/// The payload is only Base64-encoded, NOT encrypted - never put secrets in it.
/// </summary>
public class TokenService(IConfiguration configuration)
{
    public AuthResponseDto CreateToken(ApplicationUser user, IList<string> roles)
    {
        var jwt = configuration.GetSection("Jwt");

        // Short claim names keep the token small. Program.cs tells the JWT middleware
        // that "name" is the user name and "role" holds the roles.
        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("email", user.Email ?? string.Empty),
            new("name", user.FullName),
            new("given_name", user.FirstName),
            new("jti", Guid.NewGuid().ToString())   // unique id for this token
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(jwt.GetValue("ExpiresInHours", 8));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiration = expires,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }
}
