using System;
using System.Collections.Generic;
using API.DataSyncSyncer.Client.Dto;
using IntegrationEvents;
using Xunit;

namespace API.DataSyncSyncer.Persistence;

public class StateTest
{
    private readonly SyncState syncState;

    public StateTest()
    {
        Dictionary<string, DateTimeOffset> dict = new() { { "gsrn", DateTimeOffset.Now.AddDays(-1) } };

        syncState = new SyncState();
        syncState.SetState(dict);
    }

    [Fact]
    public void GetMeasurement_StateUpdated()
    {
        var currentStartTime = syncState.GetPeriodStartTime("gsrn");
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

        syncState.SetNextPeriodStartTime(fakeMeasurements, "gsrn");
        Assert.Equal(newStartTime, syncState.GetPeriodStartTime("gsrn"));
    }
}
