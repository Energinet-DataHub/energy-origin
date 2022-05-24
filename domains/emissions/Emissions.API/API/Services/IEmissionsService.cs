using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<Emissions> GetEmissions(AuthorizationContext authorizationContext, long dateFrom,
        long dateTo, Aggregation aggregation);
}