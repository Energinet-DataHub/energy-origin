using System.ComponentModel.DataAnnotations;
using API.Controllers.dto;
using API.Errors;
using API.Models;
using API.Repository;
using API.Services;
using API.Services.OidcProviders;
using API.Services.OidcProviders.Models;
using API.Services.OidcProviders.Models.SignaturGruppen;
using API.Utilities;
using EnergyOriginEventStore.EventStore;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class LoginController : ControllerBase
{
    private readonly IOidcService oidcService;
    private readonly IValidator<OidcCallbackParams> validator;
    private readonly IEventStore eventStore;

    public LoginController(
        IOidcService oidcService,
        IValidator<OidcCallbackParams> validator,
        IEventStore eventStore)
    {
        this.oidcService = oidcService;
        this.validator = validator;
        this.eventStore = eventStore;

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
    [Route("/auth/oidc/login/callback")]
    public async Task<ActionResult<NextStep>> CallbackAsync(
        OidcCallbackParams oidcCallbackParams,
        [FromServices] IUserStorage userStorage,
        [FromServices] ICompanyStorage companyStorage,
        [FromServices] ICryptographyFactory cryptographyFactory,
        [FromServices] IJwtDeserializer jwtDeserializer
    )
    {
        AuthState authState;
        IdTokenInfo oidcIdToken;
        UserInfoToken oidcUserInfoToken;
        OidcTokenResponse oidcToken;

        try
        {
            authState = cryptographyFactory.StateCryptography().Decrypt<AuthState>(oidcCallbackParams.State) ?? throw new InvalidOperationException();

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
            oidcToken = await oidcService.FetchToken(oidcCallbackParams.Code);
            oidcIdToken = jwtDeserializer.DeserializeJwt<IdTokenInfo>(oidcToken.IdToken);
            oidcUserInfoToken = jwtDeserializer.DeserializeJwt<UserInfoToken>(oidcToken.UserinfoToken);
        }
        catch (Exception)
        {
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.FailedToCommunicateWithIdentityProvider);
            return Redirect(redirectUrl.NextUrl);
        }

        if (oidcUserInfoToken.IsPrivate())
        {
            await oidcService.Logout(authState.IdToken);
            var redirectUrl = oidcService.BuildFailureUrl(authState, AuthError.PrivateUsersNotAllowedToLogin);
            return Redirect(redirectUrl.NextUrl);
        }

        var user = await userStorage.UserByOidcReferences(oidcIdToken.Sub, oidcIdToken.Idp);
        var company = (authState.Tin != null) ? await companyStorage.CompanyByTin(authState.Tin) : null;

        if (user == null)
        {
            var newAuthState = new AuthState
            {
                FeUrl = authState.FeUrl,
                ReturnUrl = authState.ReturnUrl,
                TermsAccepted = authState.TermsAccepted,
                IdToken = cryptographyFactory.IdTokenCryptography().Encrypt(oidcToken.IdToken),
                Tin = oidcUserInfoToken.NemidCvr,
                IdentityProvider = oidcIdToken.Idp,
                ExternalSubject = oidcIdToken.Sub,
                CustomerType = authState.CustomerType
            };

            var redirectUrlTerms = new NextStep
            {
                NextUrl = newAuthState.FeUrl + $"/terms?state={cryptographyFactory.StateCryptography().Encrypt(newAuthState)}"
            };
            return Redirect(redirectUrlTerms.NextUrl);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
