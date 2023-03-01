using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Helpers;

public class CustomJsonStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private readonly Dictionary<T, string> enumToString = new();
    private readonly Dictionary<string, T> stringToEnum = new();

    public CustomJsonStringEnumConverter()
    {
        var type = typeof(T);
        var values = Enum.GetValues<T>();

        foreach (var value in values)
        {
            var name = value.ToString();
            var attribute = type.GetMember(name)[0]
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                .Cast<JsonPropertyNameAttribute>()
                .FirstOrDefault();

            stringToEnum[name] = value;
            stringToEnum[name.ToLowerInvariant()] = value;

            name = attribute?.Name ?? name;
            stringToEnum[name] = value;
            stringToEnum[name.ToLowerInvariant()] = value;
            enumToString[value] = name;
        }
    }

    public override bool CanConvert(Type type) => type == typeof(T);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => stringToEnum.TryGetValue(reader.GetString()!, out var value) ? value : default;

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(enumToString[value]);
}
