using DataContext.ValueObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Domain.ValueObjects;

namespace API.MeasurementsSyncer.Persistence;

public interface IContractState
{
    Task<IReadOnlyList<MeteringPointSyncInfo>> GetSyncInfos(CancellationToken cancellationToken);
    Task DeleteContractAndSlidingWindow(Gsrn gsrn);
}
