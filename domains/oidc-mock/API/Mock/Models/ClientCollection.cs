namespace Oidc.Mock.Models;

public class ClientCollection
{
    private const string UnknownClientIdValidationError = "Unknown client_id";

    private readonly IList<Client> _clients;

    public ClientCollection(IList<Client> clients)
    {
        _clients = clients;
    }

    public (bool isValid, string validationError) Validate(string? redirectUri)
    {
        var result = _clients.FirstOrDefault(client => client.Validate(redirectUri).isValid);
        if (result is not null)
        {
            return (true, "");
        }
        return (false, "No client validated redirect url");
    }

    public (bool isValid, string validationError) Validate(string? clientId, string? redirectUri)
    {
        var client = FindClientFromClientId(clientId);
        if (client is null)
        {
            return (false, UnknownClientIdValidationError);
        }
        return client.Validate(clientId, redirectUri);
    }

    public (bool isValid, string validationError) Validate(string? clientId, string? clientSecret, string? redirectUri)
    {
        var client = FindClientFromClientId(clientId);
        if (client is null)
        {
            return (false, UnknownClientIdValidationError);
        }
        return client.Validate(clientId, clientSecret, redirectUri);
    }

    public Client? FindClientFromClientId(string? clientId)
    {
        return _clients.FirstOrDefault(c => string.Equals(clientId, c.ClientId, StringComparison.InvariantCultureIgnoreCase));
    }

}
