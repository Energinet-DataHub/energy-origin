using API.Configuration;
using API.Helpers;
using API.Models;
using API.Services;
using Jose;
using System.Text.Json;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    readonly ILogger _logger;
    readonly IOidcService _oidcService;
    readonly IOidcProviders _oidcProviders;
    readonly ICryptographyService _cryptographyService;
    private readonly AuthOptions _authOptions;
    private readonly HttpClient _httpClient;

    public Orchestrator(ILogger<Orchestrator> logger, IOidcService service, ICryptographyService cryptography, IOidcProviders oidcProviders, AuthOptions authOptions, HttpClient httpClient)
    {
        _logger = logger;
        _oidcService = service;
        _cryptographyService = cryptography;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions;
        _httpClient = httpClient;
    }
    public async void Next(AuthState state, string code)
    {

        var redirectUri = _authOptions.ServiceUrl + _authOptions.OidcLoginCallbackPath;

        var oidcToken = await _oidcService.FetchToken(state, code, redirectUri);

        var rawIdToken = DecodeJwtIdToken(oidcToken);

        //SignaturGruppenNemId userInfo;

        //// First needed when we accept private users
        //if (rawIdToken.Idp == "mitid")
        //{

        //    throw new NotSupportedException();

        //    //userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenMitId>(oidcToken.AccessToken);
        //}
        //else if (rawIdToken.Idp == "nemid")
        //{
        //    if (rawIdToken.IdentityType == "private")
        //    {
        //        // This section should return a new authorization url, pointing toward mitID and log user out from signaturgruppen.

        //        throw new NotImplementedException();
        //    }

        //    userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenNemId>(oidcToken);
        //}
        //else { throw new Exception(); } // Not sure what exception this should be


        //// Validate user creation to see wether or not the user has been created
        //var userCreated = false;
        //if (userCreated != false)
        //{
        //    // Create jwt token with actor and subject and create opaque token and return it
        //    throw new NotImplementedException();
        //}

        //// Show terms and let user accept or deny it
        //var terms = true;

        //if (terms != true)
        //{
        //    // Oidc logout backchannel
        //    _logger.LogInformation($"User {userInfo.Tin} didn't accept terms");
        //    throw new NotImplementedException();
        //}

        // Create user and company
        // EventStoreService


        // Create jwt token with actor and subject
        // Store jwt in db

        // Create opaque token and return it
    }

    private OidcTokenResponse DecodeJwtIdToken(JsonElement token)
    {
        var jwks = GetJwkAsync();
        var te = JWT.Decode(token.ToString(), jwks);

        OidcTokenResponse idToken = new OidcTokenResponse()
        {
            IdToken = token.GetProperty("id_token").GetString()!,
            AccessToken = token.GetProperty("access_token").GetString()!,
            ExpiresIn = token.GetProperty("expires_in").GetString()!,
            TokenType = token.GetProperty("token_type").GetString()!,
            Scope = token.GetProperty("scope").GetString()!,
        };

        return idToken;
    }

    private async Task<Jwk> GetJwkAsync()
    {
        var jwkResponse = await _httpClient.GetAsync($"{_authOptions.AuthorityUrl}/.well-known/openid-configuration/jwks");
        var jwkSet = JwkSet.FromJson(await jwkResponse.Content.ReadAsStringAsync(), new JsonMapper());
        var jwks = jwkSet.Keys.Single();

        return jwks;
    }
}
