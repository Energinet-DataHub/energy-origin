using API.Models;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    readonly ILogger<DataSyncService> logger;
    readonly HttpClient httpClient;

    public DataSyncService(ILogger<DataSyncService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime dateFrom, DateTime dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTime()}&dateTo={dateTo.ToUnixTime()}";

        httpClient.AddAuthorizationToken(context);

        var reponse = await httpClient.GetAsync(url);
        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, uri: {httpClient.BaseAddress} {url}");
        }

        var result = await reponse.Content.ReadFromJsonAsync<List<Measurement>>();
        if (result == null)
        {
            throw new Exception($"Parsing of meteringpoints failed. Content: {reponse.Content}");
        }

        return result;
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context)
    {
        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(context);

        var reponse = await httpClient.GetAsync(uri);
        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of meteringpoints failed, uri: {httpClient.BaseAddress} {uri}");
        }
        
        var result = await reponse.Content.ReadFromJsonAsync<MeteringPointsResponse>();
        if (result == null || result.MeteringPoints == null)
        {
            throw new Exception($"Parsing of meteringpoints failed. Content: {reponse.Content}");
        }

        return result.MeteringPoints;
    }
}
