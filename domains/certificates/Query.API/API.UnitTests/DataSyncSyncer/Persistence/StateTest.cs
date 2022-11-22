using System;
using System.Collections.Generic;
using API.DataSyncSyncer.Client.Dto;
using IntegrationEvents;
using Xunit;

namespace API.DataSyncSyncer.Persistence;

public class StateTest
{
    private readonly State state;

    public StateTest()
    {
        Dictionary<string, DateTimeOffset> dict = new();
        dict.Add("gsrn", DateTimeOffset.Now.AddDays(-1));

        state = new State();
        state.SetState(dict);
    }

    [Fact]
    public void GetMeasurement_StateUpdated()
    {
        var currentStartTime = state.GetPeriodStartTime("gsrn");
        var newStartTime = DateTimeOffset.Now.AddDays(1).ToUnixTimeSeconds();

        var fakeMeasurements = new List<DataSyncDto>()
        {
            new(
                "gsrn",
                currentStartTime,
                newStartTime,
                5,
                MeasurementQuality.Measured
            )
        };

        state.SetNextPeriodStartTime(fakeMeasurements, "gsrn");
        Assert.Equal(newStartTime, state.GetPeriodStartTime("gsrn"));
    }
}
