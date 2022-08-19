using API.Models;
using API.Services;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    readonly ILogger _logger;
    readonly IOidcService _service;
    readonly ICryptographyService _cryptographyService;
    public Orchestrator(ILogger<Orchestrator> logger, IOidcService service, ICryptographyService cryptography)
    {
        _logger = logger;
        _service = service;
        _cryptographyService = cryptography;
    }
    public async void Next(AuthState state, string code)
    {
        OidcTokenResponse oidcToken = await _service.FetchToken(state, code);

        var rawIdToken = _cryptographyService.DecodeJwt(oidcToken.IdToken);





    }

}
