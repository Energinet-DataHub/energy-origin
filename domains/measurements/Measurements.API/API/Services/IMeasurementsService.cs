using API.Models;
using EnergyOriginAuthorization;

namespace API.Services;

public interface IMeasurementsService
{
    Task<MeasurementResponse> GetMeasurements(
        AuthorizationContext context,
        TimeZoneInfo timeZone,
        DateTime dateFrom,
        DateTime dateTo,
        Aggregation aggregation,
        MeterType typeOfMP);
}
