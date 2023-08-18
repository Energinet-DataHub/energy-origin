using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using CertificateValueObjects;
using FluentAssertions;
using MeasurementEvents;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace API.UnitTests.DataSyncSyncer.Client;

public class DataSyncClientTest
{
    private const string validGsrn = "123456789012345678";
    private const string validOwner = "foo";

    private readonly MockHttpMessageHandler fakeHttpHandler = new();

    private DataSyncClient Setup()
    {
        var client = fakeHttpHandler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost:8080");

        return new DataSyncClient(
            httpClient: client,
            logger: Substitute.For<ILogger<DataSyncClient>>()
        );
    }

    [Fact]
    public async Task RequestAsync_ErrorFromDatahub_ExceptionIsThrown()
    {
        var date = DateTimeOffset.Now.AddDays(-1);

        fakeHttpHandler
            .Expect("/measurements")
            .WithQueryString("gsrn", validGsrn)
            .Respond(HttpStatusCode.InternalServerError);

        var dataSyncClient = Setup();

        await Assert.ThrowsAsync<HttpRequestException>(() => dataSyncClient.RequestAsync(
            GSRN: validGsrn,
            period: new Period(
                date.ToUnixTimeSeconds(),
                date.AddDays(1).ToUnixTimeSeconds()
            ),
            meteringPointOwner: validOwner,
            cancellationToken: CancellationToken.None
        ));

        fakeHttpHandler.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task RequestAsync_FromDatahub_DataFetched()
    {
        var date = DateTimeOffset.Now.AddDays(-1);

        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: validGsrn,
                DateFrom: date.ToUnixTimeSeconds(),
                DateTo: DateTimeOffset.Now.AddDays(-1).ToUnixTimeSeconds(),
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };
        var json = JsonConvert.SerializeObject(fakeResponseList, new StringEnumConverter());

        fakeHttpHandler
            .Expect("/measurements")
            .WithQueryString("gsrn", validGsrn)
            .Respond("application/json", json);

        var dataSyncClient = Setup();

        var response = await dataSyncClient.RequestAsync(
            GSRN: validGsrn,
            period: new Period(date.ToUnixTimeSeconds(),
                date.AddDays(1).ToUnixTimeSeconds()),
            meteringPointOwner: validOwner,
            CancellationToken.None
        );

        fakeHttpHandler.VerifyNoOutstandingExpectation();

        response.Should().Equal(fakeResponseList);
    }
}
