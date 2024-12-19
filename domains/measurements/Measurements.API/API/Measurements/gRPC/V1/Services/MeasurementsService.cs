using System.Linq;
using System.Threading.Tasks;
using API.Shared.Exceptions;
using Grpc.Core;
using Measurements.V1;
using Metertimeseries.V1;
using Microsoft.Extensions.Logging;

namespace API.Measurements.gRPC.V1.Services;

public class MeasurementsService : global::Measurements.V1.Measurements.MeasurementsBase
{
    private readonly MeterTimeSeries.MeterTimeSeriesClient _client;
    private readonly ILogger<MeasurementsService> _logger;
    private readonly MeasurementsParser _parser = new();

    public MeasurementsService(MeterTimeSeries.MeterTimeSeriesClient client, ILogger<MeasurementsService> logger)
    {
        _client = client;
        _logger = logger;
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

        var measurements = _parser.ParseMeasurements(request, dhResponse);

        var response = new GetMeasurementsResponse();
        response.Measurements.AddRange(measurements);
        return response;
    }
}
