using System.Text.Json.Serialization;
using API.Models;

namespace API.Services;

public class EnergyDataService : IEnergyDataService
{
    readonly ILogger<EnergyDataService> logger;
    readonly HttpClient httpClient;

    public EnergyDataService(ILogger<EnergyDataService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<EmissionRecord>> GetEmissionsPerHour(DateTime dateFrom, DateTime dateTo)
    {
        var query = GetEmissionsQuery(dateFrom, dateTo);

        var result = await httpClient.GetFromJsonAsync<Response<EmissionRecord>>(query) ?? throw new Exception($"EDS Emissions query failed");

        return result.Result.Records;
    }

    public async Task<IEnumerable<MixRecord>> GetResidualMixPerHour(DateTime dateFrom, DateTime dateTo)
    {
        var query = GetMixQuery(dateFrom, dateTo);

        var result = await httpClient.GetFromJsonAsync<Response<MixRecord>>(query) ?? throw new Exception($"EDS declarationproduction query failed. query:{query}");

        return result.Result.Records;
    }

    string GetMixQuery(DateTime dateFrom, DateTime dateTo)
    {
        return "datastore_search_sql?sql=SELECT \"HourUTC\", \"PriceArea\", \"Version\", \"ProductionType\", \"ShareTotal\" " +
               "from \"declarationproduction\" " +
               $"WHERE \"HourUTC\" >= '{dateFrom:MM/dd/yyyy}' AND \"HourUTC\" <= '{dateTo:MM/dd/yyyy}' AND  (\"FuelAllocationMethod\" LIKE 'All' OR \"FuelAllocationMethod\" LIKE 'Total')";
    }

    string GetEmissionsQuery(DateTime dateFrom, DateTime dateTo)
    {
        return
            $"datastore_search_sql?sql=SELECT \"PriceArea\", \"HourUTC\", \"CO2PerkWh\", \"NOxPerkWh\"  from \"declarationemissionhour\" WHERE \"HourUTC\" >= '{dateFrom:MM/dd/yyyy}' AND \"HourUTC\" <= '{dateTo:MM/dd/yyyy)}'";
    }

    private class Response<T>
    {
        public Result<T> Result { get; }

        public Response(Result<T> result)
        {
            Result = result;
        }
    }

    private class Result<T>
    {
        [JsonPropertyName("records")]
        public List<T> Records { get; }

        public List<Field> Fields { get; }

        public string Sql { get; }

        public Result(List<T> records, List<Field> fields, string sql)
        {
            Records = records;
            Fields = fields;
            Sql = sql;
        }
    }

    private class Field
    {
        public string Type { get; }

        public string Id { get; }

        public Field(string type, string id)
        {
            Type = type;
            Id = id;
        }
    }
}
