using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<MeasurementResponse> GetMeasurements(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);
}
