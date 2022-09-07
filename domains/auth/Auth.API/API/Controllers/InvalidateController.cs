using API.Models;
using API.Services.OidcProviders;
using API.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class InvalidateController : ControllerBase
{
    private readonly IOidcService oidcService;
    private readonly IValidator<AuthState?> validator;
    private readonly ICryptography stateCryptography;

    public InvalidateController(
        IOidcService oidcService,
        ICryptography stateCryptography,
        IValidator<AuthState?> validator
    )
    {
        this.oidcService = oidcService;
        this.validator = validator;
        this.stateCryptography = stateCryptography;
    }

    [HttpPost]
    [Route("/auth/invalidate")]
    public async Task<IActionResult> Invalidate([FromQuery] string state)
    {
        AuthState? authState;
        try
        {
            authState = stateCryptography.Decrypt<AuthState>(state) ?? throw new InvalidOperationException();
        }
        catch (Exception)
        {
            return BadRequest();
        }

        var validationResult = await validator.ValidateAsync(authState);

        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        await oidcService.Logout(authState.IdToken);
        return Ok();
    }
}
