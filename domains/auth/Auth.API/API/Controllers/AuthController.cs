using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Configuration;
using Microsoft.Extensions.Options;
using API.Services.OidcProviders;
using FluentValidation;

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

    public AuthController(ILogger<AuthController> logger, IOidcProviders oidcProviders,
        IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage, ICryptographyService cryptographyService,
        IValidator<AuthState> validator)
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
    public IActionResult Invalidate([FromQuery] string state)
    {
        AuthState? authState = null;
        try
        {
            var options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            authState = JsonSerializer.Deserialize<AuthState>(_cryptographyService.Decrypt(state), options);
        }
        catch (Exception e)
        {
            return BadRequest("Cannot decrypt " + nameof(AuthState));
        }

        var validationResult = _validator.Validate(authState);
        if (!validationResult.IsValid)
        {
            return BadRequest(nameof(AuthState.IdToken) + " must not be null");
        }

        _oidcProviders.Logout(authState.IdToken);
        return Ok();
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
