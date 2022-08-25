using System.ComponentModel.DataAnnotations;
using API.Configuration;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using API.Services.OidcProviders;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace API.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> logger;
    private readonly IOidcProviders oidcProviders;
    private readonly ITokenStorage tokenStorage;
    private readonly IValidator<AuthState?> _validator;
    private readonly ICryptographyService _cryptographyService;
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
        _cryptographyService = cryptographyService;
        _validator = validator;
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
            authState = _cryptographyService.Decrypt<AuthState>(state) ?? throw new InvalidOperationException();
        }
        catch (Exception e)
        {
            return BadRequest();
        }

        var validationResult = await _validator.ValidateAsync(authState);

        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        await _oidcProviders.Logout(authState.IdToken);
        return Ok();
    }

    [HttpPost]
    [Route("/auth/logout")]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            await _oidcProviders.Logout(idToken);
            _tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
