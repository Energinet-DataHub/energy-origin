using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

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

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext authorizationContext, long gsrn, DateTime dateFrom,
        DateTime dateTo, Aggregation aggregation)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTime()}&dateTo={dateTo.ToUnixTime()}&aggregation={aggregation}";
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

        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(authorizationContext);
        try
        {

            var meteringPoints = await httpClient.GetFromJsonAsync<MeteringPointsResponse>(uri);

            if (meteringPoints != null)
            {
                return meteringPoints.MeteringPoints;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
        }

        return null;
    }
}