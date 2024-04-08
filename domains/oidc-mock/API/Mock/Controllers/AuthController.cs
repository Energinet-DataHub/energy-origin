using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Web;
using API.Mock.Models;
using Microsoft.AspNetCore.Mvc;
using Oidc.Mock.Extensions;
using Oidc.Mock.Jwt;
using Oidc.Mock.Models;

namespace Oidc.Mock.Controllers;

public class AuthController : Controller
{
    private readonly Client client;
    private readonly User[] users;
    private readonly IJwtTokenGenerator tokenGenerator;
    private readonly ILogger<AuthController> logger;
    private readonly Options options;

    public AuthController(Client client, User[] users, IJwtTokenGenerator tokenGenerator, ILogger<AuthController> logger, Options options)
    {
        this.client = client;
        this.users = users;
        this.tokenGenerator = tokenGenerator;
        this.logger = logger;
        this.options = options;
    }


    [HttpGet]
    [Route("auth/demo-token")]
    public async Task<IActionResult> GetDemoToken([FromQuery] string subjectId, [FromServices] IHttpClientFactory clientFactory)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(subjectId);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        var subjectMd5 = Convert.ToHexString(hashBytes).ToUpper();
        var httpClient = clientFactory.CreateClient();

        var response = await httpClient.GetAsync("https://demo.energioprindelse.dk/api/auth/oidc/callback?code=" + subjectMd5);
        var jwt = HttpUtility.ParseQueryString(response.RequestMessage.RequestUri.Query).Get("token");
        return Ok(new {Token = jwt});
    }

    [HttpPost]
    [Route("api/v1/session/logout")]
    public IActionResult LogOut() => Ok();

    [HttpGet]
    [Route("Connect/Authorize")]
    public IActionResult Authorize(string client_id, string redirect_uri)
    {
        var (isValid, validationError) = client.Validate(client_id, redirect_uri);
        if (!isValid)
        {
            return BadRequest(validationError);
        }

        var routeValues = new RouteValueDictionary();
        foreach (var keyValuePair in Request.Query)
        {
            routeValues.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return RedirectToPage("/Connect/Signin", routeValues);
    }

    [HttpPost]
    [Route("Connect/Token")]
    public IActionResult Token(string grant_type, string code, string redirect_uri)
    {
        var authorizationHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]!);
        logger.LogDebug("connect/token: authorization header: {AuthorizationHeader}", $"{authorizationHeader.Scheme} {authorizationHeader.Parameter}");
        logger.LogDebug("connect/token: form data: {Data}", string.Join("; ", Request.Form.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        if (!string.Equals(grant_type, "authorization_code", StringComparison.InvariantCultureIgnoreCase))
        {
            return BadRequest($"Invalid grant_type. Must be 'authorization_code', but was '{grant_type}'");
        }

        var auth = (authorizationHeader.Parameter ?? ":").DecodeBase64();
        var split = auth.Split(":");
        var clientId = split[0];
        var clientSecret = split[1];

        var (isValid, validationError) = client.Validate(clientId, clientSecret, redirect_uri);
        if (!isValid)
        {
            logger.LogDebug("connect/token: {validationError}", validationError);
            return BadRequest(validationError);
        }

        if (string.IsNullOrEmpty(code))
        {
            logger.LogDebug("connect/token: Invalid code - no code given");
            return BadRequest("Invalid code - no code given");
        }

        var user = users.FirstOrDefault(u => string.Equals(u.Subject?.ToMd5(), code));
        if (user == null)
        {
            logger.LogDebug("connect/token: Invalid code - no matching user");
            return BadRequest("Invalid code - no matching user");
        }

        const int expirationInSeconds = 3600;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var baseClaims = new Dictionary<string, object>
        {
            { "iss", $"https://{options.Host}{Request.PathBase}" },
            { "iat", now },
            { "nbf", now },
            { "exp", now + expirationInSeconds },
            { "aud", clientId },
            { "scope", "openid nemid mitid userinfo_token" }
        };

        return Ok(
            new
            {
                access_token = tokenGenerator.Generate(baseClaims.Plus(user.AccessToken)),
                token_type = "Bearer",
                expires_in = expirationInSeconds,
                scope = "openid nemid mitid userinfo_token",
                id_token = tokenGenerator.Generate(baseClaims.Plus(user.IdToken)),
                userinfo_token = tokenGenerator.Generate(baseClaims.Plus(user.UserinfoToken))
            });
    }

    [HttpGet]
    [Route("connect/endsession")]
    public IActionResult logout(string post_logout_redirect_uri) => RedirectPreserveMethod(post_logout_redirect_uri);

    [HttpGet]
    [Route(".well-known/openid-configuration")]
    public IActionResult openid() =>
        Ok(new
        {
            issuer = $"https://{options.Host}{Request.PathBase}",
            jwks_uri = $"https://{options.Host}{Request.PathBase}.well-known/openid-configuration/jwks",
            authorization_endpoint = $"https://{options.Host}{Request.PathBase}connect/authorize",
            token_endpoint = $"https://{options.Host}{Request.PathBase}connect/token",
            userinfo_endpoint = $"https://{options.Host}{Request.PathBase}connect/userinfo",
            end_session_endpoint = $"https://{options.Host}{Request.PathBase}connect/endsession",
            revocation_endpoint = $"https://{options.Host}{Request.PathBase}connect/revocation",
            backchannel_authentication_endpoint = $"https://{options.Host}{Request.PathBase}connect/ciba",
            frontchannel_logout_supported = true,
            frontchannel_logout_session_supported = true,
            backchannel_logout_supported = true,
            backchannel_logout_session_supported = true,
            grant_types_supported = new[] { "authorization_code", "client_credentials", "refresh_token", "implicit", "urn:openid:params:grant-type:ciba" },
            response_types_supported = new[] { "code", "token", "id_token", "id_token token", "code id_token", "code token", "code id_token token" },
            response_modes_supported = new[] { "form_post", "query", "fragment" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post", "private_key_jwt" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            subject_types_supported = new[] { "public" },
            code_challenge_methods_supported = new[] { "plain", "RS256" },
            request_parameter_supported = true,
            request_object_signing_alg_values_supported = new[] { "RS256", "RS384", "RS512", "PS256", "PS384", "PS512", "ES256", "ES384", "ES512", "HS256", "HS384", "HS512" },
            request_uri_parameter_supported = true,
            authorization_response_iss_parameter_supported = true,
            backchannel_token_delivery_modes_supported = new[] { "poll" },
            backchannel_user_code_parameter_supported = true
        });

    [HttpGet]
    [Route(".well-known/openid-configuration/jwks")]
    public IActionResult Jwks() =>
        Ok(new
        {
            keys = new[] { tokenGenerator.GetJwkProperties() }
        });
}
