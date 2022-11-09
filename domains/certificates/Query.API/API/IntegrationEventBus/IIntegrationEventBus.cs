using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;

namespace API.DataSyncSyncer.Service.Integration;

public interface IIntegrationEventBus
{
    Task Produce(CancellationToken stoppingToken, List<EnergyMeasuredIntegrationEvent> data);
}
