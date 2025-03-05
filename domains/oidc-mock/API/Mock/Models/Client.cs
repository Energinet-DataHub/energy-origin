namespace Oidc.Mock.Models;

public record Client(string ClientId, string ClientSecret, string RedirectUri)
{
    public (bool isValid, string validationError) Validate(string? clientId, string? redirectUri)
    {
        var valid = true;
        var error = string.Empty;

        if (string.IsNullOrWhiteSpace(clientId) || !string.Equals(clientId, ClientId, StringComparison.InvariantCultureIgnoreCase))
        {
            error = "Invalid client_id";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(redirectUri) || !string.Equals(redirectUri, RedirectUri, StringComparison.InvariantCultureIgnoreCase))
        {
            error = "Invalid redirect_uri";
            valid = false;
        }

        return (valid, error);
    }

    public (bool isValid, string validationError) Validate(string? clientId, string? clientSecret, string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(clientSecret) || !string.Equals(clientSecret, ClientSecret, StringComparison.InvariantCultureIgnoreCase))
        {
            return (false, "Invalid client_secret");
        }

        var result = Validate(clientId, redirectUri);
        return !result.isValid ? result : (true, string.Empty);
    }

    public (bool isValid, string validationError) Validate(string? redirectUri)
    {
        var valid = true;
        var error = string.Empty;

        if (string.IsNullOrWhiteSpace(redirectUri) || !string.Equals(new Uri(redirectUri).Host, new Uri(RedirectUri).Host, StringComparison.InvariantCultureIgnoreCase))
        {
            error = "Invalid redirect_uri";
            valid = false;
        }

        return (valid, error);
    }
}
