using System.Text.Json;
using System.Text.Json.Serialization;

namespace HairSalon.Shared.Rules;

/// <summary>
/// JSON settings used by the Blazor client.
/// The API sends enums as text ("Pending" instead of 0) because that is easier to read in
/// Swagger. The client must therefore be told to read enums as text too - that is what the
/// JsonStringEnumConverter does. "Web" defaults = camelCase property names.
/// </summary>
public static class SalonJson
{
    public static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
