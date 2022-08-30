using API.Configuration;
using API.Models;
using API.Services.OidcProviders;
using Jose;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    private readonly ILogger logger;
    private readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;
    private readonly HttpClient httpClient;

    public Orchestrator(
        ILogger<Orchestrator> logger,
        IOidcService oidcService,
        IOptions<AuthOptions> authOptions,
        HttpClient httpClient
    )
    {
        this.logger = logger;
        this.oidcService = oidcService;
        this.authOptions = authOptions.Value;
        this.httpClient = httpClient;
    }
    public async void Next(AuthState state, string code)
    {
        var redirectUri = authOptions.ServiceUrl + authOptions.OidcLoginCallbackPath;

        var oidcToken = await oidcService.FetchToken(state, code);

        //var rawIdToken = DecodeJwtIdToken(oidcToken);




        //SignaturGruppenNemId userInfo;

        //// First needed when we accept private users
        //if (rawIdToken.Idp == "mitid")
        //{

        //    throw new NotSupportedException();

        //    //userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenMitId>(oidcToken.AccessToken);
        //}
        //else if (rawIdToken.Idp == "nemid")
        //{
        //    if (rawIdToken.IdentityType == "private")
        //    {
        //        // This section should return a new authorization url, pointing toward mitID and log user out from signaturgruppen.

        //        throw new NotImplementedException();
        //    }

        //    userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenNemId>(oidcToken);
        //}
        //else { throw new Exception(); } // Not sure what exception this should be


        //// Validate user creation to see wether or not the user has been created
        //var userCreated = false;
        //if (userCreated != false)
        //{
        //    // Create jwt token with actor and subject and create opaque token and return it
        //    throw new NotImplementedException();
        //}

        //// Show terms and let user accept or deny it
        //var terms = true;

        //if (terms != true)
        //{
        //    // Oidc logout backchannel
        //    _logger.LogInformation($"User {userInfo.Tin} didn't accept terms");
        //    throw new NotImplementedException();
        //}

        // Create user and company
        // EventStoreService


        // Create jwt token with actor and subject
        // Store jwt in db

        // Create opaque token and return it
        //return null;
    }


}
