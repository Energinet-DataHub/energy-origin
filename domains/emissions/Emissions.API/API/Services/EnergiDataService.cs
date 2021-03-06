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
        var query = GetMixQuery(dateFrom, dateTo);

        var result = await httpClient.GetFromJsonAsync<Result<MixRecord>>(query) ?? throw new Exception($"EDS declarationproduction query failed. query:'{query}'");

        return result.Records;
    }

    string GetMixQuery(DateTime dateFrom, DateTime dateTo)
    {
        return $"dataset/DeclarationProduction?start={dateFrom:yyyy-MM-dd}&end={dateTo:yyyy-MM-dd}&columns=HourUTC,PriceArea,version,ProductionType,ShareTotal";
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {

        return $"dataset/declarationemissionhour?start={dateFrom:yyyy-MM-dd}&end={dateTo:yyyy-MM-dd}&columns=HourUTC,PriceArea,CO2PerkWh,NOxPerkWh";
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
