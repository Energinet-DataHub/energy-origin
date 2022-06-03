using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext authorizationContext, long dateFrom,
        long dateTo, Aggregation aggregation);

    Task<IEnumerable<EnergySourceResponse>> GetSourceDeclaration(AuthorizationContext authorizationContext, long dateFrom, long dateTo, Aggregation aggregation);
}
