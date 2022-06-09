using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);

    Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, long dateFrom, long dateTo, Aggregation aggregation);
}
