using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.IntegrationTests.Controllers;

public static class JsonDefault
{
    public static JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };
}
