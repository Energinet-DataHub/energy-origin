using System;
using System.Threading.Tasks;
using API.Models;
using API.Models.Response;
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
