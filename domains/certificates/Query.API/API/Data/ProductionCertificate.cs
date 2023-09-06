using System;
using CertificateValueObjects;

namespace API.Data;

public class ProductionCertificate
{
    private ProductionCertificate()
    {
    }

    public ProductionCertificate(string gridArea, Period period, Technology technology, string meteringPointOwner, string gsrn, long quantity, byte[] blindingValue)
    {
        IssuedState = IssuedState.Creating;
        GridArea = gridArea;
        DateFrom = period.DateFrom;
        DateTo = period.DateTo;
        Technology = technology;
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
    public Technology Technology { get; private set; } = new("unknown", "unknown");
    public string MeteringPointOwner { get; private set; } = "";
    public string Gsrn { get; private set; } = "";
    public long Quantity { get; private set; } //TODO: long?
    public byte[] BlindingValue { get; private set; } = Array.Empty<byte>();

    public string? RejectionReason { get; private set; }
    //public byte[] WalletPublicKey { get; set; }
    //public string WalletUrl { get; set; }
    //public uint WalletDepositEndpointPosition { get; set; } //todo: Should this be saved...?

    public bool IsRejected => IssuedState == IssuedState.Rejected;
    public bool IsIssued => IssuedState == IssuedState.Issued;

    public void Reject(string reason)
    {
        IssuedState = IssuedState.Rejected;
        RejectionReason = reason;
    }

    public void Issue()
    {
        IssuedState = IssuedState.Issued;
    }
}


public enum IssuedState
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
}
