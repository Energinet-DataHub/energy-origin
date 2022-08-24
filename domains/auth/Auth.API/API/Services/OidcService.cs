using System.Text.Json;
using API.Configuration;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace API.Services;

public class OidcService : IOidcService
{
    readonly ILogger _logger;
    readonly ICryptographyService _cryptography;
    private readonly AuthOptions _authOptions;

    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, IOptions<AuthOptions> authOptions)
    {
        _logger = logger;
        _cryptography = cryptography;
        _authOptions = authOptions.Value;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);


        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", _authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", _authOptions.Scope },
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }
}
