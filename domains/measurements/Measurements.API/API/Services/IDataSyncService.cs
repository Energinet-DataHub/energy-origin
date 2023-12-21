using System.Net.Http.Headers;
using API.Models;

namespace API.Services;

public interface IDataSyncService
{
    Task<IEnumerable<Measurement>> GetMeasurements(AuthenticationHeaderValue bearerToken, string gsrn,
        DateTimeOffset dateFrom, DateTimeOffset dateTo);

    Task<IEnumerable<MeteringPoint>> GetListOfMeteringPoints(AuthenticationHeaderValue bearerToken);
}
