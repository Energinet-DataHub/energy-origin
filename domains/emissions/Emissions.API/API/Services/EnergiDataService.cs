using API.Models;
using JsonException = Newtonsoft.Json.JsonException;

namespace API.Services;

public class EnergiDataService : IEnergiDataService
{
    readonly ILogger<EnergiDataService> logger;
    readonly HttpClient httpClient;
    
    public EnergiDataService(ILogger<EnergiDataService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<EmissionsResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<EmissionsResponse>(GetEmissionsQuery(dateFrom, dateTo));
        }
        catch (JsonException e)
        {
            logger.LogError(e, null);
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, null);
        }

        return null;
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {
        return
            $"datastore_search_sql?sql=SELECT \"PriceArea\", \"HourUTC\", \"CO2PerkWh\", \"NOxPerkWh\"  from \"declarationemissionhour\" WHERE \"HourUTC\" >= '{dateFrom.ToShortDateString()}' AND \"HourUTC\" <= '{dateTo.ToShortDateString()}' ";
    }
}