using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.MasterDataService;

namespace API.DataSyncSyncer.Service.IntegrationService;

public interface IAwesomeQueue
{
    Task Produce(CancellationToken stoppingToken, List<MasterData>? data);
    Task<List<MasterData>> Consume(CancellationToken stoppingToken);
}
