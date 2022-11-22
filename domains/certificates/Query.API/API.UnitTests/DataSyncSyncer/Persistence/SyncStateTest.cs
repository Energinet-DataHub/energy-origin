using System;
using System.Collections.Generic;
using API.DataSyncSyncer.Client.Dto;
using IntegrationEvents;
using Xunit;

namespace API.DataSyncSyncer.Persistence;

public class SyncStateTest
{
    private readonly SyncState syncState;

    public SyncStateTest()
    {
        Dictionary<string, DateTimeOffset> dict = new() { { "gsrn", DateTimeOffset.Now.AddDays(-1) } };
        syncState = new SyncState(dict);
    }

    [Fact]
    public void SetNextPeriodStartTime_StateUpdated()
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
