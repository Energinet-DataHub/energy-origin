using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, DateTime dateFrom, DateTime dateTo, Aggregation aggregation);

    Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, DateTime dateFrom, DateTime dateTo, Aggregation aggregation);
}
