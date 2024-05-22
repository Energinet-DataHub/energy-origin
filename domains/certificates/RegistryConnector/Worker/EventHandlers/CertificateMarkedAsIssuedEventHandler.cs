using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataContext;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOriginClients.Models;

namespace RegistryConnector.Worker.EventHandlers;
public record CertificateMarkedAsIssuedEvent
{
    public required string WalletUrl { get; init; }
    public required byte[] WalletPublicKey { get; init; }
    public required uint WalletEndpointPosition { get; init; }
    public required string Registry { get; init; }
    public required Guid CertificateId { get; init; }
    public required uint Quantity { get; init; }
    public required byte[] RandomR { get; init; }
}

public class CertificateMarkedAsIssuedEventHandler : IConsumer<CertificateMarkedAsIssuedEvent>
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CertificateMarkedAsIssuedEventHandler> logger;

    public CertificateMarkedAsIssuedEventHandler(IHttpClientFactory httpClientFactory, ILogger<CertificateMarkedAsIssuedEventHandler> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateMarkedAsIssuedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Sending slice to Wallet with url {WalletUrl} for certificate id {certificateId}.",
            message.WalletUrl, message.CertificateId);

        var request = new ReceiveRequest()
        {
            CertificateId = new FederatedStreamId()
            {
                Registry = message.Registry,
                StreamId = message.CertificateId,
            },
            Position = message.WalletEndpointPosition,
            PublicKey = message.WalletPublicKey,
            Quantity = message.Quantity,
            RandomR = message.RandomR,
            HashedAttributes = new List<HashedAttribute>()
        };

        using var client = httpClientFactory.CreateClient();
        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var res = await client.PostAsync(message.WalletUrl, content);
        res.EnsureSuccessStatusCode();

        logger.LogInformation("Slice sent to Wallet for certificate id {certificateId}.", message.CertificateId);
    }
}

public class CertificateMarkedAsIssuedEventHandlerConsumerDefinition : ConsumerDefinition<CertificateMarkedAsIssuedEventHandler>
{
    private readonly RetryOptions retryOptions;

    public CertificateMarkedAsIssuedEventHandlerConsumerDefinition(IOptions<RetryOptions> options)
    {
        retryOptions = options.Value;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CertificateMarkedAsIssuedEventHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r
            .Incremental(retryOptions.DefaultFirstLevelRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(3)));
    }
}
