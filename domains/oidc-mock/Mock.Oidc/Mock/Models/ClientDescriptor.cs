namespace Mock.Oidc.Models;

public record ClientDescriptor(string ClientId, string ClientSecret, string RedirectUri)
{
    public (bool isValid, string validationError) Validate(string? clientId, string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(clientId) || !string.Equals(clientId, ClientId, StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, "Invalid client_id");
        }

        if (string.IsNullOrWhiteSpace(redirectUri) || !string.Equals(redirectUri, RedirectUri, StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, "Invalid redirect_uri");
        }

        return (true, string.Empty);
    }

    public (bool isValid, string validationError) Validate(string? clientId, string? clientSecret, string? redirectUri)
    {
        var result = Validate(clientId, redirectUri);
        if (!result.isValid)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(clientSecret) || !string.Equals(clientSecret, ClientSecret, StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, "Invalid client_secret");
        }

        return (true, string.Empty);
    }
}