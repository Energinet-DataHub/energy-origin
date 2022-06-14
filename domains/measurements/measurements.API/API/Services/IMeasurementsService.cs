using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<ConsumptionsResponse> GetConsumptions(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);
}
