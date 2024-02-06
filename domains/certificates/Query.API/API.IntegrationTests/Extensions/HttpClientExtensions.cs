using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace API.IntegrationTests.Extensions;

public static class HttpClientExtensions
{
    public static async Task<T?> ReadJson<T>(this HttpContent content)
    {
        var options = GetJsonSerializerOptions();
        return await content.ReadFromJsonAsync<T>(options);
    }

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
