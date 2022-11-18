using System;
using System.Collections.Generic;
using System.Linq;
using API.DataSyncSyncer.Client.Dto;
using Marten;

namespace API.DataSyncSyncer.Persistence;

public class State : IState
{
    private Dictionary<string, DateTimeOffset>? periodStartTimeDictionary;

    public void SetState(Dictionary<string, DateTimeOffset> state) => periodStartTimeDictionary = state;

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
