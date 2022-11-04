using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using API.MasterDataService;

namespace API.DataSyncSyncer.Service.IntegrationService;

public class AwesomeQueue : IAwesomeQueue
{
    private readonly ChannelWriter<List<MasterData>> channelWriter;
    private readonly ChannelReader<List<MasterData>> channelReader;

    public AwesomeQueue(
        ChannelWriter<List<MasterData>> channelWriter,
        ChannelReader<List<MasterData>> channelReader
    )
    {
        this.channelWriter = channelWriter;
        this.channelReader = channelReader;
    }

    public async Task Produce(CancellationToken stoppingToken, List<MasterData>? data)
    {
        await channelWriter.WriteAsync(data, stoppingToken);
    }

    public async Task<List<MasterData>> Consume(CancellationToken stoppingToken)
    {
        return await channelReader.ReadAsync(stoppingToken);
    }
}
