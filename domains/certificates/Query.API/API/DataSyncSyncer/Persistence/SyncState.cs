using System;
using System.Collections.Generic;
using System.Linq;
using API.DataSyncSyncer.Client.Dto;
using Marten;

namespace API.DataSyncSyncer.Persistence;

public class SyncState : ISyncState
{
    private readonly Dictionary<string, DateTimeOffset>? periodStartTimeDictionary;

    public SyncState(Dictionary<string, DateTimeOffset> periodStartTimeDictionary)
    {
        this.periodStartTimeDictionary = periodStartTimeDictionary;
    }

    public void SetNextPeriodStartTime(List<DataSyncDto> measurements, string GSRN)
    {
        if (measurements.IsEmpty())
        {
            return;
        }

        var newestMeasurement = measurements.Max(m => m.DateTo);
        periodStartTimeDictionary![GSRN] = DateTimeOffset.FromUnixTimeSeconds(newestMeasurement);
    }

    public long GetPeriodStartTime(string GSRN) => periodStartTimeDictionary![GSRN].ToUnixTimeSeconds();
}
