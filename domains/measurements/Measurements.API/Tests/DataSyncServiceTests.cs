using System;
using System.Linq;
using System.Net.Http.Headers;
using API.Models;
using API.Services;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public sealed class DataSyncServiceTests
    {
        [Fact]
        public async void DataSync_GetListOfMeteringPoints_success()
        {
            var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_meteringpoints.json");
            var datasync = new DataSyncService(mockClient);
            var token = "dummyBearerToken";
            var authHeader = new AuthenticationHeaderValue("Bearer", token);

            var res = await datasync.GetListOfMeteringPoints(authHeader);

            Assert.NotEmpty(res);
            Assert.Equal(3, res.Count());
            Assert.Equal("571313121223234323", res.First().GSRN);
            Assert.Equal("DK1", res.First().GridArea);
            Assert.Equal(MeterType.Consumption, res.First().Type);
        }

        [Fact]
        public async void DataSync_GetMeasurements_success()
        {
            var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_measurements.json");

            var dateFrom = new DateTime(2021, 1, 1);
            var dateTo = new DateTime(2021, 1, 2);

            var datasync = new DataSyncService(mockClient);

            var token = "dummyBearerToken";
            var authHeader = new AuthenticationHeaderValue("Bearer", token);

            var res = await datasync.GetMeasurements(authHeader, "571313121223234323", dateFrom, dateTo);

            Assert.NotEmpty(res);
            Assert.Equal(2, res.Count());
            Assert.Equal("571313121223234323", res.First().GSRN);
            Assert.Equal(1609455600, res.First().DateFrom);
            Assert.Equal(1609459200, res.First().DateTo);
            Assert.Equal(1250L, res.First().Quantity);
            Assert.Equal(Quality.Measured, res.First().Quality);
        }
    }
}
