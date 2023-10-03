using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using API.Models.EnergiDataService;
using API.Options;
using API.Services;
using AutoFixture;
using EnergyOriginAuthorization;
using NSubstitute;
using Tests.Helpers;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class EmissionsServiceTests
{
    private readonly EnergiDataServiceOptions options = new()
    {
        RenewableSourceList = "wood,waste,straw,bioGas,solar,windOnshore,windOffshore",
        WasteRenewableShare = 55
    };

    [Fact]
    public async void GetEmissions_3MP2Consumption_Success()
    {
        var time0 = 1654034400L;
        var time1 = 1654038000L;
        var time2 = 1654041600L;
        var time3 = 1654045200L;

        var mps = new List<MeteringPoint>(){
            new MeteringPoint("286432579631400001", "DK1", MeterType.Consumption),
            new MeteringPoint("286432579631400002", "DK1", MeterType.Production),
            new MeteringPoint("286432579631400003", "DK1", MeterType.Consumption),
        };

        var meterpoint1Time0Quantity = 1250L;
        var meterpoint1Time1Quantity = 4700L;
        var meterpoint1Time2Quantity = 2500L;

        var measurements1 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint1Time0Quantity,
                Quality: Quality.Measured
            ),
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint1Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint1Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var meterpoint2Time0Quantity = 3500L;
        var meterpoint2Time1Quantity = 1200L;
        var meterpoint2Time2Quantity = 2400L;

        var measurements2 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint2Time0Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint2Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint2Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var meterpoint3Time0Quantity = 2500L;
        var meterpoint3Time1Quantity = 700L;
        var meterpoint3Time2Quantity = 900L;

        var measurements3 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint3Time0Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint3Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint3Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var time0Co2 = 30L;
        var time1Co2 = 20L;
        var time2Co2 = 12L;

        var emissionResponse = new List<EmissionRecord>(){
            new EmissionRecord("DK1", 0, time0Co2, DateTimeOffset.FromUnixTimeSeconds(time0).DateTime),
            new EmissionRecord("DK1", 0, time1Co2, DateTimeOffset.FromUnixTimeSeconds(time1).DateTime),
            new EmissionRecord("DK1", 0, time2Co2, DateTimeOffset.FromUnixTimeSeconds(time2).DateTime),
        };

        var dataSyncService = Substitute.For<IDataSyncService>();
        dataSyncService.GetListOfMeteringPoints(
            Arg.Any<AuthorizationContext>()
        ).Returns(mps);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400001"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements1);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400002"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements2);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400003"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements3);

        var emissionsDataService = Substitute.For<IEnergiDataService>();

        emissionsDataService.GetEmissionsPerHour(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(emissionResponse);

        var service = new EmissionsService(dataSyncService, emissionsDataService, new EmissionsCalculator(), new SourcesCalculator(options));

        var result = await service.GetTotalEmissions(new AuthorizationContext("", "", ""), DateTimeOffset.FromUnixTimeSeconds(time0), DateTimeOffset.FromUnixTimeSeconds(time3), TimeZoneInfo.Utc, Aggregation.Hour);

        Assert.Equal((meterpoint1Time0Quantity + meterpoint3Time0Quantity) * time0Co2 / 1000m, result.Emissions.First().Total.Value);
        Assert.Equal((meterpoint1Time1Quantity + meterpoint3Time1Quantity) * time1Co2 / 1000m, result.Emissions.Skip(1).First().Total.Value);
        Assert.Equal((meterpoint1Time2Quantity + meterpoint3Time2Quantity) * time2Co2 / 1000m, result.Emissions.Skip(2).First().Total.Value);
    }

    [Fact]
    public async void GetMix_3MP2Consumption_Success()
    {
        var time0 = 1654034400L;
        var time1 = 1654038000L;
        var time2 = 1654041600L;
        var time3 = 1654045200L;

        var mps = new List<MeteringPoint>(){
            new MeteringPoint("286432579631400001", "DK1", MeterType.Consumption),
            new MeteringPoint("286432579631400002", "DK1", MeterType.Production),
            new MeteringPoint("286432579631400003", "DK1", MeterType.Consumption),
        };

        var meterpoint1Time0Quantity = 1250L;
        var meterpoint1Time1Quantity = 4700L;
        var meterpoint1Time2Quantity = 2500L;

        var measurements1 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint1Time0Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint1Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400001",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint1Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var meterpoint2Time0Quantity = 3500L;
        var meterpoint2Time1Quantity = 1200L;
        var meterpoint2Time2Quantity = 2400L;

        var measurements2 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint2Time0Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint2Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400002",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint2Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var meterpoint3Time0Quantity = 2500L;
        var meterpoint3Time1Quantity = 700L;
        var meterpoint3Time2Quantity = 900L;

        var measurements3 = new List<Measurement>(){
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time0,
                DateTo: time1,
                Quantity: meterpoint3Time0Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time1,
                DateTo: time2,
                Quantity: meterpoint3Time1Quantity,
                Quality: Quality.Measured
                ),
            new Measurement(
                GSRN: "286432579631400003",
                DateFrom: time2,
                DateTo: time3,
                Quantity: meterpoint3Time2Quantity,
                Quality: Quality.Measured
                ),
            };

        var mixResponse = new List<MixRecord>(){
            new MixRecord(50, DateTimeOffset.FromUnixTimeSeconds(time0).DateTime, "", "DK1", "windOnshore"),
            new MixRecord(50, DateTimeOffset.FromUnixTimeSeconds(time0).DateTime, "", "DK1", "coal"),
            new MixRecord(20, DateTimeOffset.FromUnixTimeSeconds(time1).DateTime, "", "DK1", "windOnshore"),
            new MixRecord(80, DateTimeOffset.FromUnixTimeSeconds(time1).DateTime, "", "DK1", "coal"),
            new MixRecord(70, DateTimeOffset.FromUnixTimeSeconds(time2).DateTime, "", "DK1", "windOnshore"),
            new MixRecord(30, DateTimeOffset.FromUnixTimeSeconds(time2).DateTime, "", "DK1", "coal"),
        };

        var dataSyncService = Substitute.For<IDataSyncService>();
        dataSyncService.GetListOfMeteringPoints(
            Arg.Any<AuthorizationContext>()
        ).Returns(mps);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400001"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements1);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400002"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements2);

        dataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Is<string>(x => x == "286432579631400003"),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(measurements3);

        var emissionsDataService = Substitute.For<IEnergiDataService>();
        emissionsDataService.GetResidualMixPerHour(
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(mixResponse);

        var service = new EmissionsService(dataSyncService, emissionsDataService, new EmissionsCalculator(), new SourcesCalculator(options));

        var result = await service.GetSourceDeclaration(new AuthorizationContext("", "", ""), DateTimeOffset.FromUnixTimeSeconds(time0), DateTimeOffset.FromUnixTimeSeconds(time3), TimeZoneInfo.Utc, Aggregation.Total);

        var a = result.EnergySources.First();

        Assert.Equal(0.4251m, a.Renewable);
        Assert.Equal(0.4251m, a.Ratios["windOnshore"]);
        Assert.Equal(0.5749m, a.Ratios["coal"]);
    }

    [Fact]
    public async void ListOfMeteringPoints_GetTimeSeries_Measurements()
    {
        var context = new AuthorizationContext("subject", "actor", "token");
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var measurements = StaticDataSetFactory.CreateMeasurementsFirstMP();

        var mockDataSyncService = Substitute.For<IDataSyncService>();
        var mockEds = Substitute.For<IEnergiDataService>();
        var mockEmissionsCalculator = Substitute.For<IEmissionsCalculator>();
        var mockSourcesCalculator = Substitute.For<ISourcesCalculator>();

        mockDataSyncService.GetMeasurements(
            Arg.Any<AuthorizationContext>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>()
        ).Returns(Task.FromResult(measurements.AsEnumerable()));

        var service = new EmissionsService(mockDataSyncService, mockEds, mockEmissionsCalculator, mockSourcesCalculator);

        var timeSeries = (await service.GetTimeSeries(context, dateFrom, dateTo, meteringPoints)).ToArray();

        Assert.NotNull(timeSeries);
        Assert.NotEmpty(timeSeries);
        Assert.Equal(measurements.Count, timeSeries.First().Measurements.Count());
    }
}
