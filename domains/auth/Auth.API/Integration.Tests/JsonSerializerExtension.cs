using System.Text.Json;


public class JsonSerializerExtension
{
    public static TValue? Deserialize<TValue>(string input)
    {
        return JsonSerializer.Deserialize<TValue>(input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}