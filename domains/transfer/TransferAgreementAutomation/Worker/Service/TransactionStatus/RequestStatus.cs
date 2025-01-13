using System;
using EnergyOrigin.Domain.ValueObjects;

namespace TransferAgreementAutomation.Worker.Service.TransactionStatus;

public class RequestStatus
{
    public Guid Id { get; }
    public OrganizationId SenderId { get; }
    public OrganizationId ReceiverId { get; }
    public Guid RequestId { get; }
    public UnixTimestamp RequestTimestamp { get; private set; }
    public UnixTimestamp StatusTimestamp { get; private set; }
    public Status Status { get; private set; }

    public RequestStatus(OrganizationId senderId, OrganizationId receiverId, Guid requestId, UnixTimestamp requestTimestamp, Status status = Status.Pending)
    {
        Id = Guid.NewGuid();
        SenderId = senderId;
        ReceiverId = receiverId;
        RequestId = requestId;
        StatusTimestamp = requestTimestamp;
        RequestTimestamp = requestTimestamp;
        Status = status;
    }

    public void UpdateStatus(Status updatedStatus)
    {
        Status = updatedStatus;
        StatusTimestamp = UnixTimestamp.Now();
    }
}

public enum Status
{
    Pending,
    Completed,
    Failed,
    Timeout
}
