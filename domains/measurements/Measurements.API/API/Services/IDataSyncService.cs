using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime dateFrom, DateTime dateTo, Aggregation aggregation);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context);
}
