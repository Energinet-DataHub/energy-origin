using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using EnergyOrigin.Setup;

namespace AdminPortal.Services;

public interface IMeasurementsService
{
    Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId);
}

public class MeasurementsService(HttpClient client) : IMeasurementsService
{
    public async Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId)
    {
        // return new GetMeteringpointsResponse(new List<GetMeteringPointsResponseItem>()
        // {
        //     new GetMeteringPointsResponseItem("1", MeteringPointType.Consumption),
        //     new GetMeteringPointsResponseItem("2", MeteringPointType.Production)
        // });
        client.DefaultRequestHeaders.Add(ApiVersions.HeaderName, ApiVersions.Version1);

        var response = await client.GetAsync($"meteringpoints?organizationId={organizationId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetMeteringpointsResponse>();
        return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
    }
}
