using API.Models;
using API.Services;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    readonly ILogger _logger;
    readonly IOidcService _oidcService;
    readonly IOidcProviders _oidcProviders;
    readonly ICryptographyService _cryptographyService;
    public Orchestrator(ILogger<Orchestrator> logger, IOidcService service, ICryptographyService cryptography, IOidcProviders oidcProviders)
    {
        _logger = logger;
        _oidcService = service;
        _cryptographyService = cryptography;
        _oidcProviders = oidcProviders;
    }
    public async void Next(AuthState state, string code)
    {
        OidcTokenResponse oidcToken = await _oidcService.FetchToken(state, code);

        IdTokenInfo rawIdToken = _cryptographyService.DecodeJwt<IdTokenInfo>(oidcToken.IdToken);

        SignaturGruppenNemId userInfo;

        // First needed when we accept private users
        if (rawIdToken.Idp == "mitid")
        {

            throw new NotSupportedException();

            //userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenMitId>(oidcToken.AccessToken);
        }
        else if (rawIdToken.Idp == "nemid")
        {
            if (rawIdToken.IdentityType == "private")
            {
                // This section should return a new authorization url, pointing toward mitID and log user out from signaturgruppen.

                throw new NotImplementedException();
            }

            userInfo = await _oidcProviders.FetchUserInfo<SignaturGruppenNemId>(oidcToken);
        }
        else { throw new Exception(); } // Not sure what exception this should be


        // Validate user creation to see wether or not the user has been created
        var userCreated = false;
        if (userCreated != false)
        {
            // Create jwt token with actor and subject and create opaque token and return it
            throw new NotImplementedException();
        }

        // Show terms and let user accept or deny it
        var terms = true;

        if (terms != true)
        {
            // Oidc logout backchannel
            _logger.LogInformation($"User {userInfo.Tin} didn't accept terms");
            throw new NotImplementedException();
        }

        // Create user and company
        // EventStoreService


        // Create jwt token with actor and subject
        // Store jwt in db

        // Create opaque token and return it
    }
}
