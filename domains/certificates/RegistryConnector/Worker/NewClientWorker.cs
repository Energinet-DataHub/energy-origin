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
            using var channel = GrpcChannel.ForAddress("http://localhost:7890");

            var receiveSliceServiceClient = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

            var response = await receiveSliceServiceClient.ReceiveSliceAsync(new ReceiveRequest());

            logger.LogInformation("Received {response}", response);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Bad");
            logger.LogWarning(ex.Message);
        }
    }
}
