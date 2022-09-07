namespace API.Models;
#nullable disable
public record TestOidcJwkResponse
{

    public string id_token { get; init; }

    public string access_token { get; init; }

    public string userinfo_token { get; init; }

    public int expires_in { get; init; }

    public string token_type { get; init; }

    public string scope { get; init; }
}
