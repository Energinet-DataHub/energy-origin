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

namespace RegistryConnector.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly RegisterClient registerClient;
    private readonly Key issuerKey;

    public Worker(IOptions<RegistryOptions> registryOptions, ILogger<Worker> logger)
    {
        this.logger = logger;
        registerClient = new RegisterClient(registryOptions.Value.Url);
        registerClient.Events += OnRegistryEvents;

        issuerKey = Key.Import(SignatureAlgorithm.Ed25519, registryOptions.Value.IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKeyText);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendCommand(ownerKey);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private void OnRegistryEvents(CommandStatusEvent cse)
        => logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", ToHex(cse.Id), cse.State, cse.Error);

    private async Task SendCommand(Key ownerKey)
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
