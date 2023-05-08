using System;
using System.Collections.Generic;
using Contracts.Certificates;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache
{
    public class CertificateEventsInMemoryCache : ICertificateEventsInMemoryCache
    {
        private readonly ILogger<CertificateEventsInMemoryCache> logger;
        private Dictionary<string, ProductionCertificateCreatedEvent> certificateEvents;

        public CertificateEventsInMemoryCache(ILogger<CertificateEventsInMemoryCache> logger)
        {
            this.logger = logger;
            certificateEvents = new Dictionary<string, ProductionCertificateCreatedEvent>();
        }

        public void AddCertificateWithCommandId(CommandId commandId, ProductionCertificateCreatedEvent msg) => certificateEvents.Add(HexHelper.ToHex(commandId), msg);

        public ProductionCertificateCreatedEvent? PopCertificateWithCommandId(CommandId commandId)
        {
            var hex = HexHelper.ToHex(commandId);
            if (!certificateEvents.Remove(HexHelper.ToHex(commandId), out var certificate))
            {
                logger.LogInformation($"certificate with commandId {hex} not found.");
                return null;
            }

            return certificate;
        }
    }
}
