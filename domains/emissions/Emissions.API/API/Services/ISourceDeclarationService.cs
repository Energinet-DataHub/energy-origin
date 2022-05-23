using API.Models;

namespace API.Services;

public interface ISourceDeclarationService
{
    Task<IEnumerable<GetEnergySourcesResponse>> GetSourceDeclaration(long dateFrom, long dateTo, Aggregation aggregation);
}