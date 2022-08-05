using System.Text.Json.Serialization;
using API.Models;

namespace API.Services;

// Swagger doc for EDS: https://api.energidataservice.dk/

public class EnergiDataService : IEnergiDataService
{
    readonly ILogger<EnergiDataService> logger;
    readonly HttpClient httpClient;

    public EnergiDataService(ILogger<EnergiDataService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo)
    {
        var query = GetEmissionsQuery(dateFrom, dateTo);

        var result = await httpClient.GetFromJsonAsync<Result<EmissionRecord>>(query) ?? throw new Exception($"EDS Emissions query failed. query:'{query}'");

        return result.Records;
    }

    public async Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTime dateFrom, DateTime dateTo)
    {
        var queryTotal = GetMixQueryTotal(dateFrom, dateTo);
        var queryAll = GetMixQueryAll(dateFrom, dateTo);

        var resultTotal = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryTotal) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryTotal}'");
        var resultAll = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryAll) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryAll}'");

        resultTotal.Records.AddRange(resultAll.Records);

        return resultTotal.Records;
    }

    string GetMixQueryTotal(DateTime dateFrom, DateTime dateTo)
    {

        return $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter="+"{\"FuelAllocationMethod\":\"Total\"}";
    }
    string GetMixQueryAll(DateTime dateFrom, DateTime dateTo)
    {

        return $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter=" + "{\"FuelAllocationMethod\":\"All\"}";
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {

        return $"dataset/declarationemissionhour?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,CO2PerkWh,NOxPerkWh&timezone=UTC";
    }

    private class Result<T>
    {
        [JsonPropertyName("records")]
        public List<T> Records { get; }

        public Result(List<T> records)
        {
            Records = records;
        }
    }
}
