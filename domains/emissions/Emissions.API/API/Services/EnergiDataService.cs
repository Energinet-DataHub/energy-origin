using API.Models;
using JsonException = Newtonsoft.Json.JsonException;

namespace API.Services;

public class EnergiDataService : IEnergiDataService
{
    readonly HttpClient _httpClient;
    
    public EnergiDataService(HttpClient httpClient)
    {
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

    public async Task<EmissionsResponse> GetEmissions(DateTime dateFrom, DateTime dateTo, string priceArea)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmissionsResponse>(GetEmissionsQuery(dateFrom, dateTo, priceArea));
        }
        catch (JsonException)
        {

        }
        catch (HttpRequestException)
        {
            
        }

        return null;
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo, string priceArea)
    {
        return
            $"datastore_search_sql?sql=SELECT \"HourUTC\", \"CO2PerkWh\", \"NOxPerkWh\"  from \"declarationemissionhour\" WHERE \"PriceArea\" = '{priceArea}' AND \"HourUTC\" >= '{dateFrom.ToShortDateString()}' AND \"HourUTC\" <= '{dateTo.ToShortDateString()}' ";
    }

    string GetDeclarationProductionQuery(DateTime dateTime, DateTime dateFrom, Aggregation aggregation)
    {
        return "datastore_search_sql?sql=SELECT \"HourUTC\", \"PriceArea\", \"Version\", \"ProductionType\", \"ShareTotal\" " +
               "from \"declarationproduction\" " +
               "LIMIT 10";
    }
    
}