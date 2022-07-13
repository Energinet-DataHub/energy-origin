using API.Models;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using Serilog; 

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    //readonly ILogger<DataSyncService> logger;
    readonly HttpClient httpClient;

    public DataSyncService(HttpClient httpClient)//ILogger<DataSyncService> logger, HttpClient httpClient)
    {
      //  this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime dateFrom, DateTime dateTo)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTime()}&dateTo={dateTo.ToUnixTime()}";

        httpClient.AddAuthorizationToken(context);

        var reponse = await httpClient.GetAsync(url);
        Log.Information("Reponse for {url} is {reponse} in GetMeasurements", reponse);
        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {url}");
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

        var response = await httpClient.GetAsync(uri);
        Log.Information("The response for {url} is {response} in GetListOfMeteringPoints", uri, response);
        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of meteringpoints failed, base: {httpClient.BaseAddress} url: {uri}");
        }

        var result = await response.Content.ReadFromJsonAsync<MeteringPointsResponse>();
        if (result == null || result.MeteringPoints == null)
        {
            throw new Exception($"Parsing of meteringpoints failed. Content: {response.Content}");
        }

        return result.MeteringPoints;
    }
}
