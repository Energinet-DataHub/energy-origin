using System.ComponentModel.DataAnnotations;
using API.Configuration;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> logger;
    private readonly IOidcProviders oidcProviders;
    private readonly ITokenStorage tokenStorage;
    private readonly IValidator<AuthState?> validator;
    private readonly ICryptographyService cryptographyService;
    private readonly AuthOptions authOptions;

    public AuthController(
        ILogger<AuthController> logger,
        IOidcProviders oidcProviders,
        IOptions<AuthOptions> authOptions,
        ITokenStorage tokenStorage,
        ICryptographyService cryptographyService,
        InvalidateAuthStateValidator validator
    )
    {
        this.logger = logger;
        this.oidcProviders = oidcProviders;
        this.authOptions = authOptions.Value;
        this.tokenStorage = tokenStorage;
        this.cryptographyService = cryptographyService;
        this.validator = validator;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        var state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        return oidcProviders.CreateAuthorizationUri(state);
    }

    [HttpPost]
    [Route("/invalidate")]
    public async Task<IActionResult> Invalidate([FromQuery] string state)
    {
        AuthState? authState;
        try
        {
            authState = cryptographyService.Decrypt<AuthState>(state) ?? throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            return BadRequest();
        }

        var validationResult = await validator.ValidateAsync(authState);

        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        await oidcProviders.Logout(authState.IdToken);
        return Ok();
    }

    [HttpPost]
    [Route("/auth/logout")]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            await oidcProviders.Logout(idToken);
            tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(authOptions.CookieName);

        return Ok(new LogoutResponse { Success = true });
    }
}
