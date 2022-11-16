using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Dto;
using CertificateEvents.Primitives;

namespace API.DataSyncSyncer;

public interface IDataSync
{
    Task<List<DataSyncDto>> GetMeasurement(string gsrn, Period period, string meteringPointOwner,
        CancellationToken cancellationToken);
}
