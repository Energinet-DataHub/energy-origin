using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using API.Services;
using Xunit;
using Xunit.Categories;
using Moq;
using EnergyOriginAuthorization;
using AutoFixture;
using System.Threading.Tasks;
using EnergyOriginDateTimeExtension;

namespace Tests;

[UnitTest]
public sealed class EmissionsServiceTests
{
    [Fact]
    public async void GetEmissions_3MP2Consumption_Success()
    {
        var time0 = ((long)1654034400).ToDateTime();
        var time1 = ((long)1654038000).ToDateTime();
        var time2 = ((long)1654041600).ToDateTime();
        var time3 = ((long)1654045200).ToDateTime();

        var mps = new List<MeteringPoint>(){
            new MeteringPoint("286432579631400001", "DK1", MeterType.Consumption),
            new MeteringPoint("286432579631400002", "DK1", MeterType.Production),
            new MeteringPoint("286432579631400003", "DK1", MeterType.Consumption),
        };

        var meterpoint1Time0Quantity = 1250;
        var meterpoint1Time1Quantity = 4700;
        var meterpoint1Time2Quantity = 2500;

        var measurements1 = new List<Measurement>(){
            new Measurement("286432579631400001", time0, time1, meterpoint1Time0Quantity, Quality.Measured),
            new Measurement("286432579631400001", time1, time2, meterpoint1Time1Quantity, Quality.Measured),
            new Measurement("286432579631400001", time2, time3, meterpoint1Time2Quantity, Quality.Measured),
            };

        var meterpoint2Time0Quantity = 3500;
        var meterpoint2Time1Quantity = 1200;
        var meterpoint2Time2Quantity = 2400;

        var measurements2 = new List<Measurement>(){
            new Measurement("286432579631400002", time0, time1, meterpoint2Time0Quantity, Quality.Measured),
            new Measurement("286432579631400002", time1, time2, meterpoint2Time1Quantity, Quality.Measured),
            new Measurement("286432579631400002", time2, time3, meterpoint2Time2Quantity, Quality.Measured),
            };

        var meterpoint3Time0Quantity = 2500;
        var meterpoint3Time1Quantity = 700;
        var meterpoint3Time2Quantity = 900;

        var measurements3 = new List<Measurement>(){
            new Measurement("286432579631400003", time0, time1, meterpoint3Time0Quantity, Quality.Measured),
            new Measurement("286432579631400003", time1, time2, meterpoint3Time1Quantity, Quality.Measured),
            new Measurement("286432579631400003", time2, time3, meterpoint3Time2Quantity, Quality.Measured),
            };

        var time0Co2 = 30;
        var time1Co2 = 20;
        var time2Co2 = 12;

        var emissionResponse = new List<EmissionRecord>(){
            new EmissionRecord("DK1", 0, time0Co2, time0),
            new EmissionRecord("DK1", 0, time1Co2, time1),
            new EmissionRecord("DK1", 0, time2Co2, time2),
        };

        var dataSyncService = new Mock<IDataSyncService>();
        dataSyncService.Setup(x => x.GetListOfMeteringPoints(
            It.IsAny<AuthorizationContext>()
        )).ReturnsAsync(mps);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400001"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements1);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400002"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements2);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400003"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements3);

        var emissionsDataService = new Mock<IEnergiDataService>();
        emissionsDataService.Setup(
            x => x.GetEmissionsPerHour(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )
        ).ReturnsAsync(emissionResponse);

        var service = new EmissionsService(dataSyncService.Object, emissionsDataService.Object, new EmissionsCalculator(), new SourcesCalculator());

        var result = await service.GetTotalEmissions(new AuthorizationContext("", "", ""), time0, time3, Aggregation.Hour);

        Assert.Equal(((meterpoint1Time0Quantity + meterpoint3Time0Quantity) * time0Co2) / 1000m, result.Emissions.First().Total.Value);
        Assert.Equal(((meterpoint1Time1Quantity + meterpoint3Time1Quantity) * time1Co2) / 1000m, result.Emissions.Skip(1).First().Total.Value);
        Assert.Equal(((meterpoint1Time2Quantity + meterpoint3Time2Quantity) * time2Co2) / 1000m, result.Emissions.Skip(2).First().Total.Value);
    }

    [Fact]
    public async void GetMix_3MP2Consumption_Success()
    {
        Environment.SetEnvironmentVariable("RENEWABLESOURCES", "wood,waste,straw,bioGas,solar,windOnshore,windOffshore");
        Environment.SetEnvironmentVariable("WASTERENEWABLESHARE", "55");

        var time0 = ((long)1654034400).ToDateTime();
        var time1 = ((long)1654038000).ToDateTime();
        var time2 = ((long)1654041600).ToDateTime();
        var time3 = ((long)1654045200).ToDateTime();

        var mps = new List<MeteringPoint>(){
            new MeteringPoint("286432579631400001", "DK1", MeterType.Consumption),
            new MeteringPoint("286432579631400002", "DK1", MeterType.Production),
            new MeteringPoint("286432579631400003", "DK1", MeterType.Consumption),
        };

        var meterpoint1Time0Quantity = 1250;
        var meterpoint1Time1Quantity = 4700;
        var meterpoint1Time2Quantity = 2500;

        var measurements1 = new List<Measurement>(){
            new Measurement("286432579631400001", time0, time1, meterpoint1Time0Quantity, Quality.Measured),
            new Measurement("286432579631400001", time1, time2, meterpoint1Time1Quantity, Quality.Measured),
            new Measurement("286432579631400001", time2, time3, meterpoint1Time2Quantity, Quality.Measured),
            };

        var meterpoint2Time0Quantity = 3500;
        var meterpoint2Time1Quantity = 1200;
        var meterpoint2Time2Quantity = 2400;

        var measurements2 = new List<Measurement>(){
            new Measurement("286432579631400002", time0, time1, meterpoint2Time0Quantity, Quality.Measured),
            new Measurement("286432579631400002", time1, time2, meterpoint2Time1Quantity, Quality.Measured),
            new Measurement("286432579631400002", time2, time3, meterpoint2Time2Quantity, Quality.Measured),
            };

        var meterpoint3Time0Quantity = 2500;
        var meterpoint3Time1Quantity = 700;
        var meterpoint3Time2Quantity = 900;

        var measurements3 = new List<Measurement>(){
            new Measurement("286432579631400003", time0, time1, meterpoint3Time0Quantity, Quality.Measured),
            new Measurement("286432579631400003", time1, time2, meterpoint3Time1Quantity, Quality.Measured),
            new Measurement("286432579631400003", time2, time3, meterpoint3Time2Quantity, Quality.Measured),
            };

        var mixResponse = new List<MixRecord>(){
            new MixRecord(50, time0, "", "DK1", "windOnshore"),
            new MixRecord(50, time0, "", "DK1", "coal"),
            new MixRecord(20, time1, "", "DK1", "windOnshore"),
            new MixRecord(80, time1, "", "DK1", "coal"),
            new MixRecord(70, time2, "", "DK1", "windOnshore"),
            new MixRecord(30, time2, "", "DK1", "coal"),
        };

        var dataSyncService = new Mock<IDataSyncService>();
        dataSyncService.Setup(x => x.GetListOfMeteringPoints(
            It.IsAny<AuthorizationContext>()
        )).ReturnsAsync(mps);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400001"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements1);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400002"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements2);

        dataSyncService.Setup(
            x => x.GetMeasurements(
            It.IsAny<AuthorizationContext>(),
            It.Is<string>(x => x == "286432579631400003"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        )).ReturnsAsync(measurements3);

        var emissionsDataService = new Mock<IEnergiDataService>();
        emissionsDataService.Setup(
            x => x.GetResidualMixPerHour(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()
            )
        ).ReturnsAsync(mixResponse);

        var service = new EmissionsService(dataSyncService.Object, emissionsDataService.Object, new EmissionsCalculator(), new SourcesCalculator());

        var result = await service.GetSourceDeclaration(new AuthorizationContext("", "", ""), time0, time3, Aggregation.Total);

        var a = result.EnergySources.First();

        Assert.Equal(0.4251m, a.Renewable);
        Assert.Equal(0.4251m, a.Ratios["windOnshore"]);
        Assert.Equal(0.5749m, a.Ratios["coal"]);
    }

    [Fact]
    public async void ListOfMeteringPoints_GetTimeSeries_Measurements()
    {
        //Arrange
        var context = new AuthorizationContext("subject", "actor", "token");
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = new CalculateEmissionDataSetFactory().CreateMeasurements();

        var mockDataSyncService = new Mock<IDataSyncService>();
        var mockEds = new Mock<IEnergiDataService>();
        var mockEmissionsCalculator = new Mock<IEmissionsCalculator>();
        var mockSourcesCalculator = new Mock<ISourcesCalculator>();

        mockDataSyncService.Setup(a => a.GetMeasurements(It.IsAny<AuthorizationContext>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(measurements.AsEnumerable()));

        var sut = new EmissionsService(mockDataSyncService.Object, mockEds.Object, mockEmissionsCalculator.Object, mockSourcesCalculator.Object);
        //Act

        var timeSeries = (await sut.GetTimeSeries(context, dateFrom, dateTo, meteringPoints)).ToArray();
        //Assert

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
    }
}