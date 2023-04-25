using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client.Dto;
using Domain.Certificates.Primitives;

namespace API.DataSyncSyncer.Client;

public interface IDataSyncClient
{
    Task<List<DataSyncDto>> RequestAsync(string GSRN, Period period, string meteringPointOwner,
        CancellationToken cancellationToken);
}
