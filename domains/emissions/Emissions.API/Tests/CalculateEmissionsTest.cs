using System;
using System.Collections.Generic;
using API.Helpers;
using API.Models;
using API.Services;
using AutoFixture;
using Xunit;

namespace Tests;

public class CalculateEmissionsTest
{
    readonly DateSetFactory _dateSetFactory = new();
    
    [Fact]
    public void EmissonsAndMeasurements_CalculateTotalEmission_TotalAnRelativeEmission()
    {
        var dateFrom = new DateTime(2021, 1, 1);
        var dateTo = new DateTime(2021, 1, 2);
        var meteringPoints = new Fixture().Create<List<MeteringPoint>>();
        var timeSeries = _dateSetFactory.CreateTimeSeries();
        var emissions = _dateSetFactory.CreateEmissions();
     

        var sut = new EmissionsCalculator();

        var result = sut.CalculateEmission(emissions, timeSeries, dateFrom.ToUnixTime(),
            DateTimeUtil.ToUnixTime(dateTo), aggregation);

        Assert.NotNull(result);
    }
}