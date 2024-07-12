using System;
using DataContext.ValueObjects;

namespace DataContext.Models;

public enum IssuedState
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
}

public abstract class Certificate
{
    protected Certificate() { }

    protected Certificate(string gridArea, Period period, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
    {
        Id = Guid.NewGuid();
        IssuedState = IssuedState.Creating;
        GridArea = gridArea;
        DateFrom = period.DateFrom;
        DateTo = period.DateTo;
        MeteringPointOwner = meteringPointOwner;
        Gsrn = gsrn;
        Quantity = quantity;
        BlindingValue = blindingValue;
    }

    public Guid Id { get; private set; }

    public IssuedState IssuedState { get; private set; }
    public string GridArea { get; private set; } = "";
    public long DateFrom { get; private set; }
    public long DateTo { get; private set; }
    public string MeteringPointOwner { get; private set; } = "";
    public string Gsrn { get; private set; } = "";
    public long Quantity { get; private set; }
    public byte[] BlindingValue { get; private set; } = Array.Empty<byte>();

    public string? RejectionReason { get; private set; }

    public bool IsRejected => IssuedState == IssuedState.Rejected;
    public bool IsIssued => IssuedState == IssuedState.Issued;

    public void Reject(string reason)
    {
        if (IssuedState != IssuedState.Creating)
            throw new CertificateDomainException(Id, $"Cannot reject when certificate is already {IssuedState.ToString()!.ToLower()}");

        IssuedState = IssuedState.Rejected;
        RejectionReason = reason;
    }

    public void Issue()
    {
        if (IssuedState != IssuedState.Creating)
            throw new CertificateDomainException(Id, $"Cannot issue when certificate is already {IssuedState.ToString()!.ToLower()}");

        IssuedState = IssuedState.Issued;
    }
}
