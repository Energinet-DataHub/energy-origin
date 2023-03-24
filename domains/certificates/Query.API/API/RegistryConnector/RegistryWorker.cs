using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
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
    private readonly RegistryOptions registryOptions;
    private readonly RegisterClient registerClient;
    private readonly Key ownerKey;

    public RegistryWorker(ILogger<RegistryWorker> logger, IOptions<RegistryOptions> registryOptions)
    {
        this.logger = logger;
        this.registryOptions = registryOptions.Value;

        ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        registerClient = new RegisterClient(this.registryOptions.Url);

        registerClient.Events += OnRegistryEvents;
    }

    private void OnRegistryEvents(CommandStatusEvent cse)
        => logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", ToHex(cse.Id), cse.State, cse.Error);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Key issuerKey;
        try
        {
            issuerKey = Key.Import(SignatureAlgorithm.Ed25519, registryOptions.IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKeyText);
            var rawPublicKeyBytes = issuerKey.PublicKey.Export(KeyBlobFormat.RawPublicKey);
            var rawPublicKey = ByteString.CopyFrom(rawPublicKeyBytes).ToBase64();
            logger.LogInformation("Issuer public key: {publicKey}", rawPublicKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to load issuer key. Exception: {ex}", ex);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendCommand(issuerKey);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendCommand(Key issuerKey)
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
