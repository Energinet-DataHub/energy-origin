using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public class DataSyncService : IDataSyncService
{
    readonly ILogger logger;
    readonly HttpClient httpClient;

    public DataSyncService(ILogger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext authorizationContext, long gsrn, DateTime dateFrom,
        DateTime dateTo, Aggregation aggregation)
    {
        var url = new Uri($"measurements?gsrn={gsrn}&dateFrom={dateFrom}&dateTo={dateTo}&aggregation={aggregation}");
        try
        {
            httpClient.AddAuthorizationToken(authorizationContext);
            return await httpClient.GetFromJsonAsync<List<Measurement>>(url);
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
        }
        return null;
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext authorizationContext)
    {
  
        var uri = new Uri($"GetByTin/{authorizationContext.Subject}");
        httpClient.AddAuthorizationToken(authorizationContext);
        try
        {
            var meteringPoints = await httpClient.GetFromJsonAsync<List<MeteringPoint>>(uri);
            if (meteringPoints != null)
            {
                return meteringPoints;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
        }

        return null;
    }
}