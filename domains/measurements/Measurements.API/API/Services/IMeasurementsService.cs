using API.Models;
using API.OldModels;
using API.OldModels.Response;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<MeasurementResponse> GetMeasurements(
        AuthorizationContext context,
        TimeZoneInfo timeZone,
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        Aggregation aggregation,
        MeterType typeOfMP);
}
