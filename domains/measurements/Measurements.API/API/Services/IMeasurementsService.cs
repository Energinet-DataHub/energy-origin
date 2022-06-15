using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<ConsumptionResponse> GetConsumption(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);
}
