using API.EnergySources.Models;
using API.Shared.Models;
using EnergyOriginAuthorization;

namespace API.Features.EnergySources;

public interface ISourcesService
{
    Task<EnergySourceResponse> GetSourceDeclaration(AuthorizationContext context, DateTimeOffset dateFrom, DateTimeOffset dateTo, Aggregation aggregation);
}
