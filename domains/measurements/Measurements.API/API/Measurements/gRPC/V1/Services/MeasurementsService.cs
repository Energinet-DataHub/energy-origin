using System.Linq;
using System.Threading.Tasks;
using API.Measurements.Helpers;
using API.Shared.Exceptions;
using Grpc.Core;
using Measurements.V1;
using Metertimeseries.V1;

namespace API.Measurements.gRPC.V1.Services;

public class MeasurementsService : global::Measurements.V1.Measurements.MeasurementsBase
{
    private readonly MeterTimeSeries.MeterTimeSeriesClient _client;

    public MeasurementsService(Metertimeseries.V1.MeterTimeSeries.MeterTimeSeriesClient client)
    {
        _client = client;
    }

    public override async Task<GetMeasurementsResponse> GetMeasurements(GetMeasurementsRequest request, ServerCallContext context)
    {
        var dhRequest = new MeterTimeSeriesRequest
        {
            Actor = request.Actor,
            Subject = request.Subject,
            Gsrn = request.Gsrn,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo
        };

        var dhResponse = await _client.GetMeterTimeSeriesAsync(dhRequest);

        if (dhResponse.GetMeterTimeSeriesRejection != null && dhResponse.GetMeterTimeSeriesRejection.Rejection.Count > 0)
        {
            var rejections = string.Join(
                "\n",
                dhResponse.GetMeterTimeSeriesRejection.Rejection
                    .Select(rejection => $"MeteringPoint {rejection.MeteringPointId}\n ErrorCode {rejection.ErrorCode}"));

            throw new DataHubFacadeException($"GetMeterTimeSeries was rejected\n {rejections}");
        }

        var measurements = dhResponse.GetMeterTimeSeriesResult.MeterTimeSeriesMeteringPoint.SelectMany(
            mp => mp.MeteringPointStates?.SelectMany(
                state => state.NonProfiledEnergyQuantities?.SelectMany(
                    item => item.EnergyQuantityValues?.Select(
                        quantity => new Measurement
                        {
                            Gsrn = mp.MeteringPointId,
                            DateFrom = TimeSeriesHelper.GetDateTimeFromMeterReadingOccurrence(item.Date, int.Parse(quantity.Position) - 1, state.MeterReadingOccurrence),
                            DateTo = TimeSeriesHelper.GetDateTimeFromMeterReadingOccurrence(item.Date, int.Parse(quantity.Position), state.MeterReadingOccurrence),
                            Quantity = TimeSeriesHelper.GetQuantityFromMeterReading(quantity.EnergyTimeSeriesMeasureUnit, quantity.EnergyQuantity),
                            Quality = TimeSeriesHelper.GetQuantityQualityFromMeterReading(quantity.QuantityQuality),
                        }
                    ) ?? Enumerable.Empty<Measurement>()
                )
                .GroupBy(measurement => measurement.DateFrom.ZeroedHour())
                    .Select(group => new Measurement
                    {
                        Gsrn = mp.MeteringPointId,
                        DateFrom = group.Min(m => m.DateFrom),
                        DateTo = group.Max(m => m.DateTo),
                        Quantity = group.Sum(m => m.Quantity),
                        Quality = group.Max(m => m.Quality),
                    })
                .Where(measurement => measurement.DateFrom >= request.DateFrom && measurement.DateTo <= request.DateTo)
                 ?? Enumerable.Empty<Measurement>()
            ) ?? Enumerable.Empty<Measurement>()
        ) ?? Enumerable.Empty<Measurement>();

        var response = new GetMeasurementsResponse();
        response.Measurements.AddRange(measurements);
        return response;
    }
}
