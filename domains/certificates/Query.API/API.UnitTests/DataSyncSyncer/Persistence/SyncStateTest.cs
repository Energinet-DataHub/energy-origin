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
        syncState = new SyncState();
    }

    [Fact]
    public void GetPeriodStartTime_GetTime()
    {
        var now = DateTimeOffset.Now;
        var currentStartTime = syncState.GetPeriodStartTime("gsrn", now);
        Assert.Equal(now.ToUnixTimeSeconds(), currentStartTime);
    }

    [Fact]
    public void SetNextPeriodStartTime_StateUpdated()
    {
        var currentStartTime = syncState.GetPeriodStartTime("gsrn", DateTimeOffset.Now);
        var newStartTime = DateTimeOffset.Now.AddDays(1);

        var fakeMeasurements = new List<DataSyncDto>()
        {
            new(
                "gsrn",
                currentStartTime,
                newStartTime.ToUnixTimeSeconds(),
                5,
                MeasurementQuality.Measured
            )
        };

        syncState.SetNextPeriodStartTime(fakeMeasurements, "gsrn");

        Assert.NotEqual(newStartTime.ToUnixTimeSeconds(), currentStartTime);
        Assert.Equal(newStartTime.ToUnixTimeSeconds(), syncState.GetPeriodStartTime("gsrn", newStartTime));
    }
}
