using API.Models;

namespace API.Services;

public class SourceDeclarationService : ISourceDeclarationService
{
    private readonly IEnergiDataService _energiDataService;
    private readonly IDataSyncService _dataSyncService;

    public SourceDeclarationService(IEnergiDataService energiDataService, IDataSyncService dataSyncService)
    {
        _energiDataService = energiDataService;
        _dataSyncService = dataSyncService;
    }

    public async Task<IEnumerable<GetEnergySourcesResponse>> GetSourceDeclaration(long dateFrom, long dateTo, Aggregation aggregation)
    {
        var declaration = await _energiDataService.GetDeclarationProduction(GetDateTime(dateFrom), GetDateTime(dateTo), aggregation);

        return new List<GetEnergySourcesResponse>();
    }

    private DateTime GetDateTime(long date)
    {
        return DateTimeOffset.FromUnixTimeSeconds(date).UtcDateTime;
    }
}