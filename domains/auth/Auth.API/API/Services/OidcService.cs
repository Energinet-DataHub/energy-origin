using API.Configuration;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using Jose;
using API.Controllers.dto;
using API.Errors;

namespace API.Services;

public class OidcService : IOidcService
{
    private readonly ILogger _logger;
    private readonly ICryptographyService _cryptography;
    private readonly AuthOptions _authOptions;
    private readonly HttpClient _httpClient;

    public OidcService(ILogger<OidcService> logger, ICryptographyService cryptography, IOptions<AuthOptions> authOptions, HttpClient httpClient)
    {
        _logger = logger;
        _cryptography = cryptography;
        _httpClient = httpClient;
        _authOptions = authOptions.Value;
    }

    public OidcService(ILogger logger, ICryptographyService cryptography, AuthOptions authOptions, HttpClient httpClient)
    {
        _logger = logger;
        _cryptography = cryptography;
        _authOptions = authOptions;
        _httpClient = httpClient;
    }

    public QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang)
    {
        var serilizedJson = JsonSerializer.Serialize(state);

        var query = new QueryBuilder
        {
            { "response_type", responseType },
            { "client_id", _authOptions.OidcClientId },
            { "redirect_uri", $"{state.FeUrl}/api/auth/oidc/login/callback" },
            { "scope", _authOptions.Scope},
            { "state", _cryptography.Encrypt(serilizedJson) },
            { "language", lang }
        };

        return query;
    }
}
