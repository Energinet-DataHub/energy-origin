using System.Net.Http.Headers;
using API.Models;

namespace API.Services;

public class DataSyncService(HttpClient client) : IDataSyncService
{
    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthenticationHeaderValue bearerToken, string gsrn,
        DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTimeSeconds()}&dateTo={dateTo.ToUnixTimeSeconds()}";

        client.DefaultRequestHeaders.Authorization = bearerToken;

        var reponse = await client.GetAsync(url);
        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, base: {client.BaseAddress} url: {url}");
        }

        var result = await reponse.Content.ReadFromJsonAsync<List<Measurement>>();
        return result ?? throw new Exception($"Parsing of meteringpoints failed. Content: {reponse.Content}");
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthenticationHeaderValue bearerToken)
    {
        var uri = "meteringpoints";

        client.DefaultRequestHeaders.Authorization = bearerToken;

        var response = await client.GetAsync(uri);
        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of meteringpoints failed, base: {client.BaseAddress} url: {uri}");
        }

        var result = await response.Content.ReadFromJsonAsync<MeteringPointsResponse>();
        return result?.MeteringPoints ?? throw new Exception($"Parsing of meteringpoints failed. Content: {response.Content}");
    }
}
