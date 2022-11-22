using System;
using System.Collections.Generic;
using API.DataSyncSyncer.Client.Dto;

namespace API.DataSyncSyncer.Persistence;

public interface IState
{
    void SetState(Dictionary<string, DateTimeOffset> state);
    void SetNextPeriodStartTime(List<DataSyncDto> measurements, string GSRN);
    long GetPeriodStartTime(string GSRN);
}
