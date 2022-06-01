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

    public async Task<EmissionsDataResponse> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo)
    {
        var result =await httpClient.GetFromJsonAsync<EmissionsDataResponse>(GetEmissionsQuery(dateFrom, dateTo));

        if (result != null)
        {
            return result;
        }
        throw new Exception("EDS Emissions query failed");
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {
        return
            $"datastore_search_sql?sql=SELECT \"PriceArea\", \"HourUTC\", \"CO2PerkWh\", \"NOxPerkWh\"  from \"declarationemissionhour\" WHERE \"HourUTC\" >= '{dateFrom.ToString("MM/dd/yyyy")}' AND \"HourUTC\" <= '{dateTo.ToString("MM/dd/yyyy")}' ";
    }
}