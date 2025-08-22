using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using Microsoft.Extensions.Logging;

namespace AdminPortal.Services;

public interface IMeasurementsService
{
    Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId);
}

public class MeasurementsService(HttpClient client, ILogger<MeasurementsService> logger) : IMeasurementsService
{
    public async Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId)
    {
        var response = await client.GetAsync($"internal-meteringpoints?organizationId={organizationId}");
        response.EnsureSuccessStatusCode();
        logger.LogInformation("CLIENT; " + await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<GetMeteringpointsResponse>();
        logger.LogInformation("CLIENT RESULT; " + System.Text.Json.JsonSerializer.Serialize(result?.Result));

        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
