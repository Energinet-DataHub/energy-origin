using System.Collections.Concurrent;
using System.Collections.Generic;
using Contracts.Certificates;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache;
public class CertificateEventsInMemoryCache : ICertificateEventsInMemoryCache
{
    private readonly ILogger<CertificateEventsInMemoryCache> logger;
    private readonly ConcurrentDictionary<string, MessageWrapper<ProductionCertificateCreatedEvent>> certificateEvents;

    public CertificateEventsInMemoryCache(ILogger<CertificateEventsInMemoryCache> logger)
    {
        this.logger = logger;
        certificateEvents = new ConcurrentDictionary<string, MessageWrapper<ProductionCertificateCreatedEvent>>();
    }

    public void AddCertificateWithCommandId(CommandId commandId, MessageWrapper<ProductionCertificateCreatedEvent> msg)
    {
        var hex = commandId.ToHex();
        if (!certificateEvents.TryAdd(hex, msg))
        {
            logger.LogError("Key already exist in cache. CommandId: {hex}", hex);
            throw new KeyAlreadyInCacheException(commandId);
        }
    }

    public MessageWrapper<ProductionCertificateCreatedEvent>? PopCertificateWithCommandId(CommandId commandId)
    {
        if (!certificateEvents.Remove(commandId.ToHex(), out var certificate))
        {
            return null;
        }

        return certificate;
    }
}
