using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;

namespace API.RegistryConnector;

public class RegistryWorker : BackgroundService
{
    private readonly ILogger<RegistryWorker> logger;
    private readonly RegisterClient registerClient;
    private readonly Key ownerKey;
    private readonly Key issuerKey;

    public RegistryWorker(ILogger<RegistryWorker> logger, IOptions<RegistryOptions> registryOptions)
    {
        this.logger = logger;

        ownerKey = Key.Create(SignatureAlgorithm.Ed25519);
        issuerKey = IssuerKey.LoadPrivateKey();

        registerClient = new RegisterClient(registryOptions.Value.Url);

        registerClient.Events += OnRegistryEvents;
    }

    private void OnRegistryEvents(CommandStatusEvent cse)
        => logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", ToHex(cse.Id), cse.State, cse.Error);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendCommand();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task SendCommand()
    {
        const long gsrn = 57000001234567;
        var quantity = new ShieldedValue(150);

        var commandBuilder = new ElectricityCommandBuilder();

        var federatedCertifcateId = new FederatedCertifcateId(
            "RegistryA",    // The identifier for the registry
            Guid.NewGuid()  // The unique id of the certificate, this should be saved.
        );

        commandBuilder.IssueConsumptionCertificate(
            federatedCertifcateId,
            new DateInterval(
                new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
            ),
            "DK1",
            gsrn,
            quantity,
            ownerKey.PublicKey,
            issuerKey
        );

        var commandId = await commandBuilder.Execute(registerClient);

        logger.LogInformation("Sent command. Id={id}", ToHex(commandId));
    }

    private static string ToHex(CommandId commandId)
    {
        var bytes = commandId.Hash;
        var result = new StringBuilder(bytes.Length * 2);

        foreach (var b in bytes)
            result.Append(b.ToString("x2"));

        return result.ToString();
    }

    public override void Dispose()
    {
        registerClient.Events -= OnRegistryEvents;

        base.Dispose();
    }
}
