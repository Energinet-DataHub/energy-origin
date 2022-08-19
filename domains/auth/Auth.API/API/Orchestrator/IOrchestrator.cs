using API.Models;

namespace API.Orchestrator
{
    public interface IOrchestrator
    {
        public void Next(AuthState state, string code);
    }
}
