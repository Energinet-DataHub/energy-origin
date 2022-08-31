using API.Emissions.Models;
using API.Shared.Models;
using EnergyOriginAuthorization;

namespace API.Features.Emissions;

public interface IEmissionsService
{
    Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, Aggregation aggregation);
}
