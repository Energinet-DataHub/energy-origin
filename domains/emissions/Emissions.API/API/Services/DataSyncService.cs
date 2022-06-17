using API.Helpers;
using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;
using EnergyOriginDateTimeExtension;

public class DataSyncService : IDataSyncService
{
    readonly ILogger<DataSyncService> logger;
    readonly HttpClient httpClient;

    public DataSyncService(ILogger<DataSyncService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<Measurement>> GetMeasurements(AuthorizationContext context, string gsrn, DateTime dateFrom,
        DateTime dateTo, Aggregation aggregation)
    {
        var url = $"measurements?gsrn={gsrn}&dateFrom={dateFrom.ToUnixTime()}&dateTo={dateTo.ToUnixTime()}&aggregation={aggregation}";

        httpClient.AddAuthorizationToken(context);
        var result = await httpClient.GetFromJsonAsync<List<Measurement>>(url);

        if (result != null)
        {
            return result;
        }
        throw new Exception("List of measurements failed");
    }

    public async Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthorizationContext context)
    {

        var uri = "meteringpoints";
        httpClient.AddAuthorizationToken(context);
        Console.WriteLine(await httpClient.GetStringAsync(uri));

        var meteringPoints = await httpClient.GetFromJsonAsync<MeteringPointsResponse>(uri);

        if (meteringPoints != null && meteringPoints.MeteringPoints != null)
        {
            return meteringPoints.MeteringPoints;
        }
        throw new Exception("List of meteringpoints failed");
    }
}
