using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Application.PoEventHandlers
{
    public class PoRegistryEventHandler
    {
        private readonly ILogger<PoRegistryEventHandler> logger;

        public PoRegistryEventHandler(ILogger<PoRegistryEventHandler> logger)
        {
            this.logger = logger;
        }

        public void OnRegistryEvents(CommandStatusEvent cse)
            => logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", HexHelper.ToHex(cse.Id), cse.State, cse.Error);
    }
}
