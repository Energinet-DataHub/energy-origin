using System.Text.Json.Serialization;

namespace EnergyOrigin.Datahub3;

public class Token(string accessToken, long expiresOn, int expiresIn)
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = accessToken;
    [JsonPropertyName("expires_on")]
    public long ExpiresOn { get; set; } = expiresOn;
    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; } = expiresIn;
}
