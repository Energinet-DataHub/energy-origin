using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
namespace AdminPortal.Services;

public interface IMeasurementsService
{
    Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId);
}

public class MeasurementsService(HttpClient client) : IMeasurementsService
{
    public async Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId)
    {
        var response = await client.GetAsync($"internal-meteringpoints?organizationId={organizationId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetMeteringpointsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
