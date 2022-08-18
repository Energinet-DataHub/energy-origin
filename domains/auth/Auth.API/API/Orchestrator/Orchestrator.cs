using API.Models;
using API.Services;

namespace API.Orchestrator;

public class Orchestrator : IOrchestrator
{
    readonly ILogger _logger;
    public Orchestrator(ILogger<Orchestrator> logger)
    {
        _logger = logger;
    }
    public void Next(AuthState state)
    {



    }

}
