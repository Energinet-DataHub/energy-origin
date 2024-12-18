using System;
using System.IO;
using System.Runtime.CompilerServices;
using API.Measurements.gRPC.V1.Services;
using Google.Protobuf;
using Measurements.V1;
using Metertimeseries.V1;
using Xunit;

namespace Tests.Measurements.gRPC.V1.Services;

public class MeasurementsParserTest
{
    private readonly MeasurementsParser _sut = new();

    [Fact]
    public void Bug_HourParsing()
    {
        var response = ReadResponseFile("DataHubFacadeResponse.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-11-28T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-12-05T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(168, measurements.Count);
    }

    [Fact]
    public void Bug_QuarterParsing()
    {
        var response = ReadResponseFile("DataHubFacadeResponseQuarters.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-11-28T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-11-29T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(24, measurements.Count);
    }

    [Fact]
    public void GivenQuarterMeasurements_WhenParsing_TimestampFilterIsApplied()
    {
        var response = ReadResponseFile("DataHubFacadeResponseQuarters.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-11-28T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-11-28T02:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(2, measurements.Count);
    }

    [Fact]
    public void GivenQuarterMeasurements_WhenParsing_QuarterMeasurementsAreSummed()
    {
        var response = ReadResponseFile("DataHubFacadeResponseQuarters.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-11-28T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-11-29T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(15000, measurements[0].Quantity);
        Assert.Equal(240000, measurements[1].Quantity);
        Assert.Equal(126000, measurements[22].Quantity);
    }

    [Fact]
    public void Given23MeasurementsOnADay_WhenParsing_23MeasurementsAreReturned()
    {
        var response = ReadResponseFile("DataHubFacadeResponseSwitchToDaylightSaving.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-03-31T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-04-01T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(23, measurements.Count);
        Assert.Equal(DateTimeOffset.Parse("2024-03-30T23:00:00+0000").ToUnixTimeSeconds(), measurements[0].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T00:00:00+0000").ToUnixTimeSeconds(), measurements[0].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T00:00:00+0000").ToUnixTimeSeconds(), measurements[1].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T01:00:00+0000").ToUnixTimeSeconds(), measurements[1].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T01:00:00+0000").ToUnixTimeSeconds(), measurements[2].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T02:00:00+0000").ToUnixTimeSeconds(), measurements[2].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T02:00:00+0000").ToUnixTimeSeconds(), measurements[3].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T03:00:00+0000").ToUnixTimeSeconds(), measurements[3].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T03:00:00+0000").ToUnixTimeSeconds(), measurements[4].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T04:00:00+0000").ToUnixTimeSeconds(), measurements[4].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T04:00:00+0000").ToUnixTimeSeconds(), measurements[5].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T05:00:00+0000").ToUnixTimeSeconds(), measurements[5].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T05:00:00+0000").ToUnixTimeSeconds(), measurements[6].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T06:00:00+0000").ToUnixTimeSeconds(), measurements[6].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-03-31T21:00:00+0000").ToUnixTimeSeconds(), measurements[22].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-03-31T22:00:00+0000").ToUnixTimeSeconds(), measurements[22].DateTo);
    }

    [Fact]
    public void Given25MeasurementsOnADay_WhenParsing_25MeasurementsAreReturned()
    {
        var response = ReadResponseFile("DataHubFacadeResponseSwitchFromDaylightSaving.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-10-26T22:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-10-28T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(25, measurements.Count);
        Assert.Equal(DateTimeOffset.Parse("2024-10-26T22:00:00+0000").ToUnixTimeSeconds(), measurements[0].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-26T23:00:00+0000").ToUnixTimeSeconds(), measurements[0].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-26T23:00:00+0000").ToUnixTimeSeconds(), measurements[1].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T00:00:00+0000").ToUnixTimeSeconds(), measurements[1].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T00:00:00+0000").ToUnixTimeSeconds(), measurements[2].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T01:00:00+0000").ToUnixTimeSeconds(), measurements[2].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T01:00:00+0000").ToUnixTimeSeconds(), measurements[3].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T02:00:00+0000").ToUnixTimeSeconds(), measurements[3].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T02:00:00+0000").ToUnixTimeSeconds(), measurements[4].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T03:00:00+0000").ToUnixTimeSeconds(), measurements[4].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T03:00:00+0000").ToUnixTimeSeconds(), measurements[5].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T04:00:00+0000").ToUnixTimeSeconds(), measurements[5].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T04:00:00+0000").ToUnixTimeSeconds(), measurements[6].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T05:00:00+0000").ToUnixTimeSeconds(), measurements[6].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T21:00:00+0000").ToUnixTimeSeconds(), measurements[23].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T22:00:00+0000").ToUnixTimeSeconds(), measurements[23].DateTo);

        Assert.Equal(DateTimeOffset.Parse("2024-10-27T22:00:00+0000").ToUnixTimeSeconds(), measurements[24].DateFrom);
        Assert.Equal(DateTimeOffset.Parse("2024-10-27T23:00:00+0000").ToUnixTimeSeconds(), measurements[24].DateTo);
    }

    [Fact]
    public void Given92MeasurementsOnADay_WhenParsing_23MeasurementsAreReturned()
    {
        var response = ReadResponseFile("DataHubFacadeResponseSwitchToDaylightSavingQuarters.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-03-31T00:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-04-01T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(23, measurements.Count);
    }

    [Fact]
    public void Given100MeasurementsOnADay_WhenParsing_25MeasurementsAreReturned()
    {
        var response = ReadResponseFile("DataHubFacadeResponseSwitchFromDaylightSavingQuarters.json");
        var parser = new JsonParser(JsonParser.Settings.Default);
        var parsedResponse = parser.Parse<MeterTimeSeriesResponse>(response);

        var from = DateTimeOffset.Parse("2024-10-26T22:00:00+0100").ToUnixTimeSeconds();
        var to = DateTimeOffset.Parse("2024-10-28T00:00:00+0100").ToUnixTimeSeconds();
        var measurements = _sut.ParseMeasurements(new GetMeasurementsRequest() { Gsrn = "571313111111111111", DateFrom = from, DateTo = to },
            parsedResponse);

        Assert.Equal(25, measurements.Count);
    }

    private string ReadResponseFile(string file, [CallerFilePath] string sourceFile = "")
    {
        var directory = Path.GetDirectoryName(sourceFile);
        return File.ReadAllText(Path.Join(directory, file));
    }
}
