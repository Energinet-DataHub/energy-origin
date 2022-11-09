using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;

namespace API.DataSyncSyncer.Service.Datasync;

public interface IDataSync
{
    Task<List<DataSyncDto>> GetMeasurement(string gsrn, Period period, string meteringPointOwner);
}
