using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;

namespace AdminPortal.Utilities.Local;

public class MockMeasurementsService : IMeasurementsService
{
    public Task<GetMeteringpointsResponse> GetMeteringPointsHttpRequestAsync(Guid organizationId)
    {
        if (MockData.MeteringPoints.TryGetValue(organizationId, out var meteringPoints))
        {
            return Task.FromResult(new GetMeteringpointsResponse(meteringPoints));
        }

        return Task.FromResult(new GetMeteringpointsResponse(new List<GetMeteringPointsResponseItem>()));
    }
}
