using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    private readonly ILogger _logger;
    readonly HttpClient _httpClient;
    
    public DataSyncService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext authorizationContext, long gsrn, DateTime dateFrom,
        DateTime dateTo, Aggregation aggregation)
    {
        var url = new Uri($"measurements?gsrn={gsrn}&dateFrom={dateFrom}&dateTo={dateTo}&aggregation={aggregation}");
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationContext.Token);
            return await _httpClient.GetFromJsonAsync<List<Measurement>>(url);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
        }
        return null;
    }

    public async Task<IEnumerable<long>> GetListOfMeteringPoints(AuthorizationContext authorizationContext)
    {
  
        var uri = new Uri($"GetByTin/{authorizationContext.Subject}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationContext.Token);
        try
        {
            var meteringPoints = await _httpClient.GetFromJsonAsync<List<MeteringPoint>>(uri);
            if (meteringPoints != null)
            {
                return meteringPoints.Select(_ => _.Gsrn);
            }
        }
        catch (Exception e)
        {
            throw;
        }

        return null;
    }
}