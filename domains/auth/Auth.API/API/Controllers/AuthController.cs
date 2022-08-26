using API.Configuration;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using API.Controllers.dto;
using Microsoft.AspNetCore.Http.Extensions;
using API.Orchestrator;
using System.Reflection;
using API.Errors;

namespace API.Controllers;


[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> _logger;
    readonly IOidcProviders _oidcProviders;
    private IOidcService _oidcService;
    readonly ITokenStorage _tokenStorage;
    private readonly AuthOptions _authOptions;
    private readonly IOrchestrator _orchestrator;

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders, IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, IOrchestrator orchestrator, IOidcService oidcService)
    {
        _logger = logger;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions.Value;
        _tokenStorage = tokenStorage;
        _orchestrator = orchestrator;
        _oidcService = oidcService;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string fe_url,
        [Required] string return_url)
    {
        AuthState state = new AuthState()
        {
            FeUrl = fe_url,
            ReturnUrl = return_url
        };

        return _oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpGet]
    [Route("/oidc/login/callback")]
    public async Task<NextStep> CallbackAsync(OidcCallbackParams oidcCallbackParams)
    {
        AuthState? authState = new AuthState();

        try
        {
            authState = JsonSerializer.Deserialize<AuthState>(oidcCallbackParams.State, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            BadRequest(nameof(OidcCallbackParams.State) + " is not valid. Exception:" + ex);
        }

        if (_oidcProviders.isError(oidcCallbackParams))
        {
            var redirectlocation = _oidcProviders.OnOidcFlowFailed(authState, oidcCallbackParams);
            //HttpContext.Response.Headers.Add("StatusCode", "307");
            HttpContext.Response.Redirect(redirectlocation.NextUrl);
        }

        var redirectUri = _authOptions.ServiceUrl + _authOptions.OidcLoginCallbackPath;
        try
        {
            var oidcToken = await _oidcProviders.FetchToken(authState, oidcCallbackParams.Code, redirectUri);
        }
        catch (Exception ex)
        {
            var redirectUrl = _oidcProviders.BuildFailureUrl(authState, AuthError.FailedToCommunicateWithIdentityProvider);
            HttpContext.Response.Redirect(redirectUrl.NextUrl);
        }






        //_orchestrator.Next(authState, oidcCallbackParams.Code);

        return new NextStep();
    }





    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            //TODO _oidcProviders.Logout(idToken);
            _tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
