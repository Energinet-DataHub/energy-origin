using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;

namespace API.DataSyncSyncer.Service.Integration;

public class IntegrationEventBus : IIntegrationEventBus
{
    private readonly ChannelWriter<EnergyMeasuredIntegrationEvent> channelWriter;

    public IntegrationEventBus(
        ChannelWriter<EnergyMeasuredIntegrationEvent> channelWriter
    )
    {
        this.channelWriter = channelWriter;
    }

    public async Task Produce(CancellationToken stoppingToken, EnergyMeasuredIntegrationEvent data)
    {
        await channelWriter.WriteAsync(data, stoppingToken);
    }
}
