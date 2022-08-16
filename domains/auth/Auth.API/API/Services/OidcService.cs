using API.Configuration;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace API.Services;

public class OidcService : IOidcService
{
    private readonly ILogger _logger;
    private readonly ICryptographyService _cryptography;
    private readonly AuthOptions _authOptions;

    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, IOptions<AuthOptions> authOptions)
    {
        _logger = logger;
        _cryptography = cryptography;
        _authOptions = authOptions.Value;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", _authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", _authOptions.Scope },
            { "state", _cryptography.EncryptState(state.ToString()) },
            { "language", lang }
        };

        return query;
    }
}
