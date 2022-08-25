using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using API.Configuration;
using Microsoft.Extensions.Options;
using API.Services.OidcProviders;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace API.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    readonly ILogger<AuthController> _logger;
    readonly IOidcProviders _oidcProviders;
    readonly ITokenStorage _tokenStorage;
    private readonly IValidator<AuthState?> _validator;
    private readonly ICryptographyService _cryptographyService;
    private readonly AuthOptions _authOptions;

    public AuthController(
        ILogger<AuthController> logger,
        IOidcProviders oidcProviders,
        IOptions<AuthOptions> authOptions,
        ITokenStorage tokenStorage,
        ICryptographyService cryptographyService,
        InvalidateAuthStateValidator validator
    )
    {
        _logger = logger;
        _oidcProviders = oidcProviders;
        _authOptions = authOptions.Value;
        _tokenStorage = tokenStorage;
        _cryptographyService = cryptographyService;
        _validator = validator;
    }

    [HttpGet]
    [Route("/oidc/login")]
    public NextStep Login(
        [Required] string feUrl,
        [Required] string returnUrl)
    {
        AuthState state = new AuthState()
        {
            FeUrl = feUrl,
            ReturnUrl = returnUrl
        };

        return _oidcProviders.CreateAuthorizationUri(state);
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
            return BadRequest("Cannot decrypt " + nameof(AuthState));
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
        var opaqueToken = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            await _oidcProviders.Logout(idToken);
            _tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
