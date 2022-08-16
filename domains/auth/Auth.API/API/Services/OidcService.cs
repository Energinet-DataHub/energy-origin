using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using API.Services;

namespace API.Services;
public class OidcService : IOidcService
{
    readonly ILogger _logger;
    readonly ICryptographyService _cryptography;
    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography)
    {
        _logger = logger;
        _cryptography = cryptography;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", Configuration.GetOidcClientId() },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", Configuration.GetScopes() },
            { "state", _cryptography.EncryptState(state) },
            { "language", lang }
        };

        return query;
    }
}
