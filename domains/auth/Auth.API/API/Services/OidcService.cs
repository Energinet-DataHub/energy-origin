using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using API.Services;

namespace API.Services;
public class OidcService : IOidcService
{
    readonly ILogger _logger;
    readonly ICryptographyService _cryptographyService;
    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography)
    {
        _logger = logger;
        _cryptographyService = cryptography;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var query = new QueryBuilder();

        query.Add("response_type", responseType);
        query.Add("client_id", Configuration.GetOidcClientId());
        query.Add("redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback");
        query.Add("scope", Configuration.GetScopes());
        query.Add("state", _cryptographyService.EncryptState(state));
        query.Add("language", lang);

        return query;
    }
}
