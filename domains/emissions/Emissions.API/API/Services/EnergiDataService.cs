using API.Models;
using JsonException = Newtonsoft.Json.JsonException;

namespace API.Services;

public class EnergiDataService : IEnergiDataService
{
    private readonly ILogger _logger;
    readonly HttpClient _httpClient;
    
    public EnergiDataService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }
    public async Task<DeclarationProduction> GetDeclarationProduction(DateTime dateFrom, DateTime dataTo, Aggregation aggregation)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DeclarationProduction>(GetDeclarationProductionQuery(dateFrom, dataTo, aggregation));
        }
        catch (JsonException)
        {

        }
        catch (HttpRequestException)
        {
            
        }

        return null;
    }

    public async Task<EmissionsResponse> GetEmissions(DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmissionsResponse>(GetEmissionsQuery(dateFrom, dateTo));
        }
        catch (JsonException)
        {

        }
        catch (HttpRequestException)
        {
            
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