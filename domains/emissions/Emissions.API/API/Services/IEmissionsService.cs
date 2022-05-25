using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<IEnumerable<Emissions>> GetTotalEmissions(AuthorizationContext authorizationContext, long dateFrom,
        long dateTo, Aggregation aggregation);
}