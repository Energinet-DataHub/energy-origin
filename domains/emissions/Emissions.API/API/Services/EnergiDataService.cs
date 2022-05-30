using API.Models;
using JsonException = Newtonsoft.Json.JsonException;

namespace API.Services;

public class EnergiDataService : IEnergiDataService
{
    readonly ILogger logger;
    readonly HttpClient httpClient;
    
    public EnergiDataService(ILogger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }
    public async Task<DeclarationProduction> GetDeclarationProduction(DateTime dateFrom, DateTime dataTo, Aggregation aggregation)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<DeclarationProduction>(GetDeclarationProductionQuery(dateFrom, dataTo, aggregation));
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

    string GetDeclarationProductionQuery(DateTime dateTime, DateTime dateFrom, Aggregation aggregation)
    {
        return "datastore_search_sql?sql=SELECT \"HourUTC\", \"PriceArea\", \"Version\", \"ProductionType\", \"ShareTotal\" " +
               "from \"declarationproduction\" " +
               "LIMIT 10";
    }
    
}