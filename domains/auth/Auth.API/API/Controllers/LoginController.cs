using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using API.Controllers.dto;
using API.Errors;
using API.Helpers;
using API.Models;
using API.Orchestrator;
using API.Repository;
using API.Services.OidcProviders;
using EnergyOriginEventStore.EventStore;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    private readonly IOidcService oidcService;
    private readonly IValidator<OidcCallbackParams> validator;
    private readonly ICryptography cryptography;
    private readonly IEventStore eventStore;
    private readonly IUserStorage userStorage;
    private readonly ICompanyStorage companyStorage;
    private readonly IOrchestrator orchestrator;

    public LoginController(IOidcService oidcService, IValidator<OidcCallbackParams> validator, ICryptography cryptography, IEventStore eventStore, IUserStorage userStorage, ICompanyStorage companyStorage, IOrchestrator orchestrator)
    {
        this.oidcService = oidcService;
        this.validator = validator;
        this.cryptography = cryptography; // FIXME! We need two of these!
        this.eventStore = eventStore;
        this.userStorage = userStorage;
        this.companyStorage = companyStorage;
        this.orchestrator = orchestrator;
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
        AuthState authState;
        IdTokenInfo oidcIdToken;
        UserInfoToken oidcUserInfoToken;
        OidcTokenResponse oidcToken;
        try
        {
            authState = cryptography.Decrypt<AuthState>(oidcCallbackParams.State) ?? throw new InvalidOperationException();

        }
        catch (Exception)
        {
            return BadRequest();
        }

        var validationResult = await validator.ValidateAsync(oidcCallbackParams);

        if (!validationResult.IsValid)
        {
            var redirectlocation = oidcService.OnOidcFlowFailed(authState, oidcCallbackParams);
            return Redirect(redirectlocation.NextUrl);
        }

        try
        {
            oidcToken = await oidcService.FetchToken(authState, oidcCallbackParams.Code);
            oidcIdToken = DeserializeToken<IdTokenInfo>(oidcToken.IdToken);
            oidcUserInfoToken = DeserializeToken<UserInfoToken>(oidcToken.UserinfoToken);
        }
        catch (Exception)
        {
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.FailedToCommunicateWithIdentityProvider);
            return Redirect(redirectUrl.NextUrl);
        }

        if (oidcUserInfoToken.IsPrivate)
        {
            await oidcService.Logout(authState.IdToken);
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.PrivateUsersNotAllowedToLogin);
            return Redirect(redirectUrl.NextUrl);
        }

        authState = new AuthState
        {
            FeUrl = authState.FeUrl,
            ReturnUrl = authState.ReturnUrl,
            TermsAccepted = authState.TermsAccepted,
            IdToken = cryptography.Encrypt(oidcToken.IdToken),
            Tin = oidcUserInfoToken.NemidCvr,
            IdentityProvider = oidcIdToken.Idp,
            ExternalSubject = oidcIdToken.Sub,
            CustomerType = authState.CustomerType
        };


        var user = await userStorage.UserByOidcReferences(oidcIdToken.Sub, oidcIdToken.Idp);
        var company = (authState.Tin != null) ? await companyStorage.CompanyByTin(authState.Tin) : null;

        return await orchestrator.Next(authState, user, company);
    }

    internal T DeserializeToken<T>(string token)
    {
        var jwt = new JwtSecurityToken(token);
        var json = jwt.Payload.SerializeToJson();
        var info = JsonSerializer.Deserialize<T>(json);

        return info ?? throw new FormatException();
    }
}
