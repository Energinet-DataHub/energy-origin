using System.Collections.Generic;
using Contracts.Certificates;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache;
public class CertificateEventsInMemoryCache : ICertificateEventsInMemoryCache
{
    private readonly ILogger<CertificateEventsInMemoryCache> logger;
    private readonly Dictionary<string, MessageWrapper<ProductionCertificateCreatedEvent>> certificateEvents;

    public CertificateEventsInMemoryCache(ILogger<CertificateEventsInMemoryCache> logger)
    {
        this.logger = logger;
        certificateEvents = new Dictionary<string, MessageWrapper<ProductionCertificateCreatedEvent>>();
    }

    public void AddCertificateWithCommandId(CommandId commandId, MessageWrapper<ProductionCertificateCreatedEvent> msg) => certificateEvents.Add(HexHelper.ToHex(commandId), msg);

    public MessageWrapper<ProductionCertificateCreatedEvent>? PopCertificateWithCommandId(CommandId commandId)
    {
        if (!certificateEvents.Remove(HexHelper.ToHex(commandId), out var certificate))
        {
            return null;
        }

        return certificate;
    }
}
