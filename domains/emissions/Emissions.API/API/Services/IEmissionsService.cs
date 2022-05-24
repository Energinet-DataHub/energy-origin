using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<IEnumerable<GetEmissionsResponse>> GetEmissions(AuthorizationContext authorizationContext, long dateFrom,
        long dateTo, Aggregation aggregation);
}