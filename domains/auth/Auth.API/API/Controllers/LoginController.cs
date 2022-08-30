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
using API.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Collections.Generic;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    private readonly IOidcService oidcService;
    private readonly IValidator<OidcCallbackParams> validator;
    private readonly AuthOptions authOptions;
    private readonly ICryptography cryptography;

    public LoginController(IOidcService oidcService, IValidator<OidcCallbackParams> validator, IOptions<AuthOptions> authOptions, ICryptography cryptography)
    {
        this.oidcService = oidcService;
        this.validator = validator;
        this.authOptions = authOptions.Value;
        this.cryptography = cryptography;
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
    public async Task<ActionResult<NextStep>> CallbackAsync(OidcCallbackParams oidcCallbackParams)
    {
        var authState = new AuthState();

        try
        {
            authState = cryptography.Decrypt<AuthState>(oidcCallbackParams.State) ?? throw new InvalidOperationException();

        }
        catch (Exception ex)
        {
            BadRequest();
        }

        var validationResult = await validator.ValidateAsync(oidcCallbackParams);

        if (!validationResult.IsValid)
        {
            var redirectlocation = oidcService.OnOidcFlowFailed(authState, oidcCallbackParams);
            return Redirect(redirectlocation.NextUrl);
            
        }

        try
        {
            var oidcToken = await oidcService.FetchToken(authState, oidcCallbackParams.Code);

            var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(oidcToken.IdToken);
            var idTokenInfo = new IdTokenInfo() {
                Iss = jwt.Claims.First(claim => claim.Type == "iss").Value,
                Nbf = int.Parse(jwt.Claims.First(claim => claim.Type == "nbf").Value),
                Iat = int.Parse(jwt.Claims.First(claim => claim.Type == "iat").Value),
                Exp = int.Parse(jwt.Claims.First(claim => claim.Type == "exp").Value),
                Aud = jwt.Claims.First(claim => claim.Type == "aud").Value,
                Amr = (List<string>)jwt.Claims.Select(claim => claim.Type == "amr"),
                Sub = jwt.Claims.First(claim => claim.Type == "sub").Value,
                AuthTime = int.Parse(jwt.Claims.First(claim => claim.Type == "auth_time").Value),
                Idp = jwt.Claims.First(claim => claim.Type == "idp").Value,
                NebSid = jwt.Claims.First(claim => claim.Type == "neb_sid").Value,
                Aal = jwt.Claims.First(claim => claim.Type == "aal").Value,
                IdentityType = jwt.Claims.First(claim => claim.Type == "identity_type").Value,
                TransactionId = jwt.Claims.First(claim => claim.Type == "transaction_id").Value,
                IdpTransactionId = jwt.Claims.First(claim => claim.Type == "idp_transaction_id").Value,
                SessionExpiry = jwt.Claims.First(claim => claim.Type == "session_expiry").Value,
            };

            //authState.Tin;
        }
        catch (Exception ex)
        {
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.FailedToCommunicateWithIdentityProvider);
            return Redirect(redirectUrl.NextUrl);
        }

        if (authState.CustomerType == "private")
        {
            await oidcService.Logout(authState.IdToken);
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.PrivateUsersNotAllowedToLogin);
            return Redirect(redirectUrl.NextUrl);
        }





        //orchestrator.Next(authState, oidcCallbackParams.Code);

        return new NextStep();
    }
}
