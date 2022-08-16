using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public sealed class DataSyncServiceTest
    {
        [Fact]
        public async void DataSync_GetListOfMeteringPoints_success()
        {
            // Arrange
            var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_meteringpoints.json");

            var logger = new Mock<ILogger<DataSyncService>>();

            var datasync = new DataSyncService(logger.Object, mockClient);

            // Act
            var res = await datasync.GetListOfMeteringPoints(new AuthorizationContext("", "", ""));

            // Assert
            Assert.NotEmpty(res);
            Assert.Equal(3, res.Count());
            Assert.Equal("571313121223234323", res.First().GSRN);
            Assert.Equal("DK1", res.First().GridArea);
            Assert.Equal(MeterType.Consumption, res.First().Type);
        }

        [Fact]
        public async void DataSync_GetMeasurements_success()
        {
            // Arrange
            var mockClient = MockHttpClientFactory.SetupHttpClientFromFile("datasync_measurements.json");

            var dateFrom = new DateTime(2021, 1, 1);
            var dateTo = new DateTime(2021, 1, 2);
            var logger = new Mock<ILogger<DataSyncService>>();

            var datasync = new DataSyncService(logger.Object, mockClient);

            // Act
            var res = await datasync.GetMeasurements(new AuthorizationContext("", "", ""), "571313121223234323", dateFrom, dateTo);

            // Assert
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
