using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.WalletSystem.V1;

namespace RegistryConnector.Worker;

public class NewClientWorker : BackgroundService
{
    private readonly ILogger<NewClientWorker> logger;

    public NewClientWorker(ILogger<NewClientWorker> logger)
        => this.logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:1234");
            //var walletServiceClient = new WalletService.WalletServiceClient(channel);
            //walletServiceClient.CreateWalletAsync(new CreateWalletRequest { });

            var externalWalletServiceClient = new ExternalWalletService.ExternalWalletServiceClient(channel);

            var respsone = await externalWalletServiceClient.ReceiveSliceAsync(new ReceiveRequest(), cancellationToken: stoppingToken);

            logger.LogInformation("Received {response}", respsone);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Bad");
            logger.LogWarning(ex.Message);
        }
    }
}
