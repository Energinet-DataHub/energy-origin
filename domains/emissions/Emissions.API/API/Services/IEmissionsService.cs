using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IEmissionsService
{
    Task<EmissionsResponse> GetTotalEmissions(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, TimeZoneInfo timeZone, Aggregation aggregation);
    Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, TimeZoneInfo timeZone, Aggregation aggregation);
}
