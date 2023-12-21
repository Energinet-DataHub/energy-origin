using System;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;

namespace API.Transfer.Api.Metrics;

public class TransferMetrics
{
    private  Counter<int> TransfersCreatedCounter { get; }
    private  Counter<int> TransfersEditedCounter { get; }
    private  UpDownCounter<int> TotalTransfersUpDownCounter { get; }

    public TransferMetrics(IMeterFactory meterFactory, IConfiguration configuration)
    {
        var meter = meterFactory.Create(configuration["TransferAgreementMeterName"] ??
                                        throw new NullReferenceException("Transfer meter missing a name"));

        TransfersCreatedCounter = meter.CreateCounter<int>("transfers-created", "TransferAgreement");
        TransfersEditedCounter = meter.CreateCounter<int>("transfers-updated", "TransferAgreement");
        TotalTransfersUpDownCounter = meter.CreateUpDownCounter<int>("total-transfers", "TransferAgreement");
    }

    public void IncreaseTransfers() => TransfersCreatedCounter.Add(1);
    public void UpdateTransfer() => TransfersEditedCounter.Add(1);
    public void IncreaseTotalTransfers() => TotalTransfersUpDownCounter.Add(1);
    public void DecreaseTotalTransfers() => TotalTransfersUpDownCounter.Add(-1);
}
