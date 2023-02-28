using System.Text.Json;
using System.Text.Json.Serialization;
using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web);

    public DataSyncService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        options.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTimeSeconds()}&dateTo={dateTo.ToUnixTimeSeconds()}";

        httpClient.AddAuthorizationToken(context);
        var result = await httpClient.GetFromJsonAsync<List<Measurement>>(url, options);

        return result ?? throw new Exception("List of measurements failed");
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context)
    {
        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(context);

        var result = await httpClient.GetFromJsonAsync<MeteringPointsResponse>(uri, options);

        return result?.MeteringPoints ?? throw new Exception("List of meteringpoints failed");
    }
}
