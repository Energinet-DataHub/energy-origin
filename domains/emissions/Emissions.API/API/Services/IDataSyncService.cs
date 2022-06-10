using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext authorizationContext, string gsrn, DateTime utcDateTime,
        DateTime dateTime, Aggregation aggregation);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext authorizationContext);
}
