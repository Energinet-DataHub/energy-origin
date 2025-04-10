using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;

namespace AdminPortal.Services;

public interface IMeasurementsService
{
    Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(List<Guid> organizationIds);
}

public class MeasurementsService(HttpClient client) : IMeasurementsService
{
    public async Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(List<Guid> organizationIds)
    {
        var response = await client.PostAsJsonAsync("internal-meteringpoints/", organizationIds);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetMeteringpointsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
