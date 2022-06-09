using System.Text.Json;
using API.Models;

namespace API.Services;

public class EmissionDataService : IEmissionDataService
{
    readonly ILogger<EmissionDataService> logger;
    readonly HttpClient httpClient;

    public EmissionDataService(ILogger<EmissionDataService> logger, HttpClient httpClient)
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

    public async Task<DeclarationProduction> GetDeclarationProduction(DateTime dateFrom, DateTime dataTo)
    {
        var result = await httpClient.GetFromJsonAsync<DeclarationProduction>(GetQuery(dateFrom, dataTo));
        if (result != null)
        {
            return result;
        }

        throw new Exception($"EDS declarationproduction query failed. query:{GetQuery(dateFrom, dataTo)}");
    }

    string GetQuery(DateTime dateFrom, DateTime dateTo)
    {

        return "datastore_search_sql?sql=SELECT \"HourUTC\", \"PriceArea\", \"Version\", \"ProductionType\", \"ShareTotal\" " +
               "from \"declarationproduction\" " +
               $"WHERE \"HourUTC\" >= '{dateFrom:MM/dd/yyyy)}' AND \"HourUTC\" <= '{dateTo:MM/dd/yyyy}' AND  (\"FuelAllocationMethod\" LIKE 'All' OR \"FuelAllocationMethod\" LIKE 'Total')";
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {
        return
            $"datastore_search_sql?sql=SELECT \"PriceArea\", \"HourUTC\", \"CO2PerkWh\", \"NOxPerkWh\"  from \"declarationemissionhour\" WHERE \"HourUTC\" >= '{dateFrom:MM/dd/yyyy)}' AND \"HourUTC\" <= '{dateTo:MM/dd/yyyy)}'";
    }
}
