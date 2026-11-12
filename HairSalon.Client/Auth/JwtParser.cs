using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HairSalon.Client.Auth;

/// <summary>
/// Reads the claims out of a JWT in the browser.
/// A JWT is "header.payload.signature"; the payload is Base64Url-encoded JSON such as
///   { "sub": "ab12...", "name": "Anna Svensson", "role": "Customer", "exp": 1767225600 }
///
/// The client does NOT verify the signature (it doesn't know the secret key). That's fine:
/// the claims are only used to decide what to SHOW. The API verifies the token on every request,
/// so editing the token in the browser gives no extra access to data.
/// </summary>
public static class JwtParser
{
    public static List<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new FormatException("The token is not a valid JWT.");

        var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();

        var claims = new List<Claim>();
        foreach (var (type, value) in values)
        {
            // Several roles are stored as an array: "role": ["Admin", "Customer"]
            if (value.ValueKind == JsonValueKind.Array)
                claims.AddRange(value.EnumerateArray().Select(item => new Claim(type, item.ToString())));
            else
                claims.Add(new Claim(type, value.ToString()));
        }
        return claims;
    }

    /// <summary>Base64Url uses - and _ instead of + and / and leaves out the = padding.</summary>
    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => ""
        };
        return Convert.FromBase64String(base64);
    }
}
