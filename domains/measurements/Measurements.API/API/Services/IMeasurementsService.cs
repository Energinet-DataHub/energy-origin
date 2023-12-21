using System.Net.Http.Headers;
using API.Models;

namespace API.Services;

public interface IMeasurementsService
{
    Task<MeasurementResponse> GetMeasurements(
        AuthenticationHeaderValue bearerToken,
        TimeZoneInfo timeZone,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        Aggregation aggregation,
        MeterType typeOfMP);
}
