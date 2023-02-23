using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTimeOffset dateFrom, DateTimeOffset dateTo);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context);
}
