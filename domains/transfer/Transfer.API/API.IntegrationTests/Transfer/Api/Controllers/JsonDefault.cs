using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.IntegrationTests.Transfer.Api.Controllers;

public static class JsonDefault
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };
}
