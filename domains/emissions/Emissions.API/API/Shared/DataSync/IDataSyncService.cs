using API.Shared.DataSync.Models;
using EnergyOriginAuthorization;

namespace API.Shared.DataSync;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTimeOffset utcDateTime,
        DateTimeOffset dateTime);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context);
}
