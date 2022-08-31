using System.Text.Json.Serialization;
using API.Shared.EnergiDataService.Models;

namespace API.Shared.EnergiDataService;

// Swagger doc for EDS: https://api.energidataservice.dk/

public class EnergiDataService : IEnergiDataService
{
    private readonly HttpClient httpClient;

    public EnergiDataService(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var query = GetEmissionsQuery(dateFrom.ToUniversalTime(), dateTo.ToUniversalTime());

        var result = await httpClient.GetFromJsonAsync<Result<EmissionRecord>>(query) ?? throw new Exception($"EDS Emissions query failed. query:'{query}'");

        return result.Records;
    }

    public async Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var queryTotal = GetMixQueryTotal(dateFrom.ToUniversalTime(), dateTo.ToUniversalTime());
        var queryAll = GetMixQueryAll(dateFrom.ToUniversalTime(), dateTo.ToUniversalTime());

        var resultTotal = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryTotal) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryTotal}'");
        var resultAll = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryAll) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryAll}'");

        resultTotal.Records.AddRange(resultAll.Records);

        return resultTotal.Records;
    }

    private static string GetMixQueryTotal(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter=" + "{\"FuelAllocationMethod\":\"Total\"}";

    private static string GetMixQueryAll(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter=" + "{\"FuelAllocationMethod\":\"All\"}";

    private static string GetEmissionsQuery(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/declarationemissionhour?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,CO2PerkWh,NOxPerkWh&timezone=UTC";

    private class Result<T>
    {
        [JsonPropertyName("records")]
        public List<T> Records { get; }

        public Result(List<T> records) => Records = records;
    }
}
