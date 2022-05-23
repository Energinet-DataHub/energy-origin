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
            return await _httpClient.GetFromJsonAsync<DeclarationProduction>(GetQuery(dateFrom, dataTo, aggregation));
        }
        catch (JsonException)
        {

        }
        catch (HttpRequestException)
        {
            
        }

        return null;
    }

    string GetQuery(DateTime dateTime, DateTime dateFrom, Aggregation aggregation)
    {
        return "datastore_search_sql?sql=SELECT \"HourUTC\", \"PriceArea\", \"Version\", \"ProductionType\", \"ShareTotal\" " +
               "from \"declarationproduction\" " +
               "LIMIT 10";
    }
    
}