using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using API.Configuration;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Orchestrator;
using API.Services;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    private readonly IOidcService oidcService;
    private readonly IOrchestrator orchestrator;
    private readonly AuthOptions authOptions;

    public LoginController(IOidcService oidcService, IOrchestrator orchestrator, AuthOptions authOptions)
    {
        this.oidcService = oidcService;
        this.orchestrator = orchestrator;
        this.authOptions = authOptions;
    }

    [HttpGet]
    [Route("/auth/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        var state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        return oidcService.CreateAuthorizationUri(state);
    }

    [HttpGet]
    [Route("/oidc/login/callback")]
    public async Task<NextStep> CallbackAsync(OidcCallbackParams oidcCallbackParams)
    {
        var authState = new AuthState();

        try
        {
            authState = JsonSerializer.Deserialize<AuthState>(oidcCallbackParams.State, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            BadRequest(nameof(OidcCallbackParams.State) + " is not valid. Exception:" + ex);
        }

        if (oidcService.isError(oidcCallbackParams))
        {
            var redirectlocation = oidcService.OnOidcFlowFailed(authState, oidcCallbackParams);
            //HttpContext.Response.Headers.Add("StatusCode", "307");
            HttpContext.Response.Redirect(redirectlocation.NextUrl);
        }

        var redirectUri = authOptions.ServiceUrl + authOptions.OidcLoginCallbackPath;
        try
        {
            var oidcToken = await oidcService.FetchToken(authState, oidcCallbackParams.Code, redirectUri);
        }
        catch (Exception ex)
        {
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.FailedToCommunicateWithIdentityProvider);
            HttpContext.Response.Redirect(redirectUrl.NextUrl);
        }

        orchestrator.Next(authState, oidcCallbackParams.Code);

        return new NextStep();
    }
}
