using System.Text.Json;
using API.Configuration;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace API.Services;

public class OidcService : IOidcService
{
    readonly ILogger logger;
    readonly ICryptographyService cryptography;
    private readonly AuthOptions authOptions;

    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, IOptions<AuthOptions> authOptions)
    {
        this.logger = logger;
        this.cryptography = cryptography;
        this.authOptions = authOptions.Value;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);


        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", authOptions.Scope },
            { "state", cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }
}
