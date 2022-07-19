using System.Text.Json;

namespace Oidc.Mock.Extensions;

public static class JsonExtensions
{
    public static void AddFromJsonFile<T>(this IServiceCollection services, string jsonFilePath) where T : class
    {
        using var reader = new StreamReader(jsonFilePath);
        var json = reader.ReadToEnd();
        var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result == null)
        {
            throw new Exception($"Could not deserialize JSON file '{jsonFilePath}' to {typeof(T)}");
        }

        services.AddSingleton<T>(_ => result);
    }
}
