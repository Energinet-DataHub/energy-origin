using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.Extensions.Logging;
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
            new("571313131313131314",
                MeteringPointType.Production,
                "982",
                SubMeterType.Physical,
                new Address("Some vej 124", null, null, "Aarhus C", "8000", "Denmark", "0751", "Aarhus"),
                new Dtos.Response.Technology("Some tech code", "Some fuel code"),
                "12345678",
                true,
                "1234567",
                "DK1")
        });
        var organizationId = Guid.NewGuid();

        var mockHttp = new MockHttpMessageHandler();

        mockHttp.When("http://localhost/internal-meteringpoints?organizationId=" + organizationId)
            .Respond(HttpStatusCode.OK, new StringContent(JsonConvert.SerializeObject(mockResponse)));
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");
        var logger = NSubstitute.Substitute.For<ILogger<MeasurementsService>>();
        var measurementsService = new MeasurementsService(client, logger);

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
        var logger = NSubstitute.Substitute.For<ILogger<MeasurementsService>>();

        var measurementsService = new MeasurementsService(client, logger);

        var result = await measurementsService.GetMeteringPointsHttpRequestAsync(organizationId);
        Assert.Equivalent(mockResponse, result);
    }
}
