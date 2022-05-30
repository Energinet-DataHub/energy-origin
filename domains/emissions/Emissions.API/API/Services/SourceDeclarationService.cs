using API.Models;

namespace API.Services;

public class SourceDeclarationService : ISourceDeclarationService
{
    readonly IEnergiDataService energiDataService;
    readonly IDataSyncService dataSyncService;

    public SourceDeclarationService(IEnergiDataService energiDataService, IDataSyncService dataSyncService)
    {
        this.energiDataService = energiDataService;
        this.dataSyncService = dataSyncService;
    }

    public async Task<IEnumerable<GetEnergySourcesResponse>> GetSourceDeclaration(long dateFrom, long dateTo, Aggregation aggregation)
    {
        var declaration = await energiDataService.GetDeclarationProduction(GetDateTime(dateFrom), GetDateTime(dateTo), aggregation);

        return new List<GetEnergySourcesResponse>();
    }

    DateTime GetDateTime(long date)
    {
        return DateTimeOffset.FromUnixTimeSeconds(date).UtcDateTime;
    }
}