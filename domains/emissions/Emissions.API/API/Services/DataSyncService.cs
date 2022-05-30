using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    readonly ILogger _logger;
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
            _httpClient.AddAuthorizationToken(authorizationContext);
            return await _httpClient.GetFromJsonAsync<List<Measurement>>(url);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
        }
        return null;
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext authorizationContext)
    {
  
        var uri = new Uri($"GetByTin/{authorizationContext.Subject}");
        _httpClient.AddAuthorizationToken(authorizationContext);
        try
        {
            var meteringPoints = await _httpClient.GetFromJsonAsync<List<MeteringPoint>>(uri);
            if (meteringPoints != null)
            {
                return meteringPoints;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
        }

        return null;
    }
}