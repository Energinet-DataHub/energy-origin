using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.MasterDataService;
using CertificateEvents;
using CertificateEvents.Primitives;

namespace API.DataSyncSyncer.Service.Datasync;

public interface IDataSync
{
    Task<EnergyMeasuredIntegrationEvent> GetMeasurement(string gsrn, Period period);
}
