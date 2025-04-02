using System;
using System.Collections.Generic;
using System.Net.Http;
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
        return new GetMeteringpointsResponse(new List<GetMeteringPointsResponseItem>() {
            new GetMeteringPointsResponseItem("1", MeteringPointType.Consumption, "test", "12345678"),
            new GetMeteringPointsResponseItem("2", MeteringPointType.Production, "test", "12345678"),
        });
    }
}
