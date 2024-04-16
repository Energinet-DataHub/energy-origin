using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace API.MeasurementsSyncer.Persistence;

public interface IContractState
{
    Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken);
}
