using System.Text.Json;
using API.Models;
using API.Models.EnergiDataService;

namespace API.Services;

// Swagger doc for EDS: https://api.energidataservice.dk/index.html

public class EnergiDataService : IEnergiDataService
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

    public EnergiDataService(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var query = GetEmissionsQuery(dateFrom, dateTo);

        var result = await httpClient.GetFromJsonAsync<Result<EmissionRecord>>(query, options) ?? throw new Exception($"EDS Emissions query failed. query:'{query}'");

        return result.Records;
    }

    public async Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var queryTotal = GetMixQueryTotal(dateFrom, dateTo);
        var queryAll = GetMixQueryAll(dateFrom, dateTo);

        var resultTotal = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryTotal, options) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryTotal}'");
        var resultAll = await httpClient.GetFromJsonAsync<Result<MixRecord>>(queryAll, options) ?? throw new Exception($"EDS declarationproduction query failed. query:'{queryAll}'");

        resultTotal.Records.AddRange(resultAll.Records);

        return resultTotal.Records;
    }

    private static string GetMixQueryTotal(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter=" + "{\"FuelAllocationMethod\":\"Total\"}";
    private static string GetMixQueryAll(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal&timezone=UTC&filter=" + "{\"FuelAllocationMethod\":\"All\"}";
    private static string GetEmissionsQuery(DateTimeOffset dateFrom, DateTimeOffset dateTo) => $"dataset/declarationemissionhour?start={dateFrom:yyyy-MM-ddTHH:mm}&end={dateTo:yyyy-MM-ddTHH:mm}&columns=HourUTC,PriceArea,CO2PerkWh,NOxPerkWh&timezone=UTC";

    private class Result<T>
    {
        public List<T> Records { get; }

        public Result(List<T> records) => Records = records;
    }
}
