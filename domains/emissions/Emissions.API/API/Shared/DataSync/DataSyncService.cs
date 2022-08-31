using API.Extensions;
using API.Shared.DataSync.Models;
using EnergyOriginAuthorization;

namespace API.Shared.DataSync;

public class DataSyncService : IDataSyncService
{
    private readonly HttpClient httpClient;

    public DataSyncService(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTimeSeconds()}&dateTo={dateTo.ToUnixTimeSeconds()}";

        httpClient.AddAuthorizationToken(context);
        var result = await httpClient.GetFromJsonAsync<List<Measurement>>(url);

        return result ?? throw new Exception("List of measurements failed");
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context)
    {
        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(context);

        var result = await httpClient.GetFromJsonAsync<MeteringPointsResponse>(uri);

        return result?.MeteringPoints ?? throw new Exception("List of meteringpoints failed");
    }
}
