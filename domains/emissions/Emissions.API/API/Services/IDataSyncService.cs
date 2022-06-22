using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime utcDateTime,
        DateTime dateTime);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context);
}
