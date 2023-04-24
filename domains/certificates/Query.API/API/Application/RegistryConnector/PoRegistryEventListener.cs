using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;

namespace API.Application.RegistryConnector
{
    public class PoRegistryEventListener
    {
        private readonly ILogger<PoRegistryEventListener> logger;

        public PoRegistryEventListener(ILogger<PoRegistryEventListener> logger)
        {
            this.logger = logger;
        }

        public void OnRegistryEvents(CommandStatusEvent cse)
            => logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", HexHelper.ToHex(cse.Id), cse.State, cse.Error);
    }
}
