using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<MeasurementResponse> GetConsumption(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);

    Task<MeasurementResponse> GetProduction(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);
}