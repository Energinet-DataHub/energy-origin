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

        var reponse = await httpClient.GetAsync(url);
        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {url}");
        }

        var result = await reponse.Content.ReadFromJsonAsync<List<Measurement>>();
        return result ?? throw new Exception($"Parsing of meteringpoints failed. Content: {reponse.Content}");
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context)
    {
        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(context);

        var response = await httpClient.GetAsync(uri);
        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of meteringpoints failed, base: {httpClient.BaseAddress} url: {uri}");
        }

        var result = await response.Content.ReadFromJsonAsync<MeteringPointsResponse>();
        return result?.MeteringPoints ?? throw new Exception($"Parsing of meteringpoints failed. Content: {response.Content}");
    }
}
