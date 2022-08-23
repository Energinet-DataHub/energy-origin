using API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Services.OidcProviders;
using FluentValidation;

namespace API.Controllers;

[ApiController]
public class InvalidateController : ControllerBase
{
    readonly IOidcService _oidcService;
    private readonly IValidator<AuthState?> _validator;
    private readonly ICryptography _cryptography;

    public InvalidateController(IOidcService oidcService, ICryptography cryptography,
        IValidator<AuthState> validator)
    {
        _oidcService = oidcService;
        _validator = validator;
        _cryptography = cryptography;
    }

    [HttpPost]
    [Route("/invalidate")]
    public IActionResult Invalidate([FromQuery] string state)
    {
        AuthState? authState = null;
        try
        {
            authState = JsonSerializer.Deserialize<AuthState>(
                _cryptography.Decrypt(state),
                new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
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

        _oidcService.Logout(authState.IdToken);
        return Ok();
    }
}
