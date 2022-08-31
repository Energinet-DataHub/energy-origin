using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    private readonly HttpClient httpClient;

    public DataSyncService(HttpClient httpClient) => this.httpClient = httpClient;

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime dateFrom, DateTime dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTime()}&dateTo={dateTo.ToUnixTime()}";

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
