using API.Models;

namespace API.Orchestrator
{
    public interface IOrchestrator
    {
        public Task<NextStep> Next(AuthState state, User? user, Company? company);
    }
}
