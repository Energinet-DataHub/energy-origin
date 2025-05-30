using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;

namespace AdminPortal.Tests.Services;

public class MeasurementsServiceTest
{
    [Fact]
    public async Task GetMeteringPointsHttpRequestAsync_WithMeteringpoints_ReturnsResult()
    {
        var mockResponse = new GetMeteringpointsResponse(new List<GetMeteringPointsResponseItem>()
        {
            new("GSRN", MeteringPointType.Consumption)
        });
        var organizationId = Guid.NewGuid();

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When("http://localhost/internal-meteringpoints?organizationId=" + organizationId)
            .Respond(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(mockResponse)));
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var measurementsService = new MeasurementsService(client);

        var result = await measurementsService.GetMeteringPointsHttpRequestAsync(organizationId);
        Assert.Equivalent(mockResponse, result);
    }

    [Fact]
    public async Task GetMeteringPointsHttpRequestAsync_NoMeteringpoints_ReturnsEmptyResult()
    {
        var mockResponse = new GetMeteringpointsResponse([]);
        var organizationId = Guid.NewGuid();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://localhost/internal-meteringpoints?organizationId=" + organizationId)
            .Respond(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(mockResponse)));
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");

        var measurementsService = new MeasurementsService(client);

        var result = await measurementsService.GetMeteringPointsHttpRequestAsync(organizationId);
        Assert.Equivalent(mockResponse, result);
    }
}
