using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using API.Configuration;
using API.Controllers.dto;
using API.Errors;
using API.Helpers;
using API.Models;
using API.Services.OidcProviders;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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
            var idTokeninfo = ClaimToken(oidcToken);


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

    public IdTokenInfo ClaimToken(OidcTokenResponse oidcToken)
    {



        //var idTokenInfo = new IdTokenInfo();
        //var jwt = JwtExtensions.GetJwtPayload(oidcToken.IdToken);
        //var idToken = JsonDocument.Parse(jwt).RootElement;
        //idTokenInfo = new IdTokenInfo
        //{
        //    Iss = idToken.GetProperty("iss").GetString(),
        //    Nbf = idToken.GetProperty("nbf").GetInt32(),
        //    Iat = idToken.GetProperty("iat").GetInt32(),
        //    Exp = idToken.GetProperty("exp").GetInt32(),
        //    Aud = idToken.GetProperty("aud").GetString(),
        //    Amr = idToken.GetProperty("amr").EnumerateArray(),
        //    Sub = idToken.GetProperty("sub").GetString(),
        //    AuthTime = idToken.GetProperty("auth_time").GetInt32(),
        //    Idp = idToken.GetProperty("idp").GetString(),
        //    NebSid = idToken.GetProperty("neb_sid").GetString(),
        //    Aal = idToken.GetProperty("aal").GetString(),
        //    IdentityType = idToken.GetProperty("identity_type").GetString(),
        //    TransactionId = idToken.GetProperty("transaction_id").GetString(),
        //    IdpTransactionId = idToken.GetProperty("idp_transaction_id").GetString(),
        //    SessionExpiry = idToken.GetProperty("session_expiry").GetString()
        //};


        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadToken(oidcToken.IdToken) as JwtSecurityToken;
        var role = token.Claims.First(claim => claim.Type == "iss").Value;
        var roles = token.Claims.First(claim => claim.Type == "nbf").Value;
        // var role = token.Claims.First(claim => claim.Type == "iss").Value;



        //idTokenInfo = new IdTokenInfo()
        //{
        //    Iss = jwt.Claims.First(claim => claim.Type == "iss").Value,
        //    //Nbf = int.Parse(jwt.Claims.First(claim => claim.Type == "nbf").Value),
        //    //Iat = int.Parse(jwt.Claims.First(claim => claim.Type == "iat").Value),
        //    //Exp = int.Parse(jwt.Claims.First(claim => claim.Type == "exp").Value),
        //    //Aud = jwt.Claims.First(claim => claim.Type == "aud").Value,
        //    //Amr = (List<string>)jwt.Claims.Select(claim => claim.Type == "amr"),
        //    //Sub = jwt.Claims.First(claim => claim.Type == "sub").Value,
        //    //AuthTime = int.Parse(jwt.Claims.First(claim => claim.Type == "auth_time").Value),
        //    //Idp = jwt.Claims.First(claim => claim.Type == "idp").Value,
        //    //NebSid = jwt.Claims.First(claim => claim.Type == "neb_sid").Value,
        //    //Aal = jwt.Claims.First(claim => claim.Type == "aal").Value,
        //    //IdentityType = jwt.Claims.First(claim => claim.Type == "identity_type").Value,
        //    //TransactionId = jwt.Claims.First(claim => claim.Type == "transaction_id").Value,
        //    //IdpTransactionId = jwt.Claims.First(claim => claim.Type == "idp_transaction_id").Value,
        //    //SessionExpiry = jwt.Claims.First(claim => claim.Type == "session_expiry").Value,
        //};
        var idTokenInfo = new IdTokenInfo();

        return idTokenInfo;
    }
}


//var jwt = JwtExtensions.GetJwtPayload(oidcToken.IdToken);
// var idTokenInfo = new IdTokenInfo();

//var idToken = JsonDocument.Parse(jwt).RootElement;
//var idTokenInfo = new IdTokenInfo {
//    Iss = idToken.GetProperty("iss").GetString(),
//    Nbf = int.Parse(idToken.GetProperty("nbf").GetString()),
//    Iat = int.Parse(idToken.GetProperty("iat").GetString()),
//    Exp = int.Parse(idToken.GetProperty("exp").GetString()),
//    Aud = idToken.GetProperty("aud").GetString(),

//    //Iss = idToken.GetProperty("iss").GetString(),
//    //  Nbf = int.Parse(idToken.GetProperty("nbf").GetString())
//};
