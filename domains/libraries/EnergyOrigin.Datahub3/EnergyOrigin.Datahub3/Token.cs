using System.Text.Json.Serialization;

namespace EnergyOrigin.Datahub3;

public class Token
{
    public Token(string accessToken, int expires)
    {
        AccessToken = accessToken;
        Expires = expires;
    }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int Expires { get; set; }
}
