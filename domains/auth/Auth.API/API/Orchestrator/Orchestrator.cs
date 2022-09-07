using API.Configuration;
using API.Models;
using API.Services.OidcProviders;
using Microsoft.Extensions.Options;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    private readonly ILogger logger;
    private readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;

    public Orchestrator(
        ILogger<Orchestrator> logger,
        IOidcService oidcService,
        IOptions<AuthOptions> authOptions
    )
    {
        this.logger = logger;
        this.oidcService = oidcService;
        this.authOptions = authOptions.Value;
    }

    public Task<NextStep> Next(AuthState authState, User? user, Company? company)  // FIXME 
    {
        if (user == null)
        {
            var redirectUrl = new NextStep { NextUrl = authState.FeUrl + "/terms" };

            return Task.FromResult(redirectUrl);

        }

        if (user != null)
        {
            throw new NotImplementedException();
        }


        throw new NotImplementedException();

    }


}
