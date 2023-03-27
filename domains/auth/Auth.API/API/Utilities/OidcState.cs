using System.Text;
using System.Text.Json;

namespace API.Utilities;

public record OidcState(string? State, string? RedirectionUri)
{
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static OidcState? Decode(string? encoded)
    {
        if (encoded == null)
        {
            return null;
        }
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        return JsonSerializer.Deserialize<OidcState>(json);
    }
}
