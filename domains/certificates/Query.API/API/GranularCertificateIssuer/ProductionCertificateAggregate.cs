using System;
using System.Numerics;
using CertificateEvents;
using CertificateEvents.Primitives;

namespace API.GranularCertificateIssuer;

public class ProductionCertificateAggregate : AggregateBase
{
    private ProductionCertificateAggregate()
    {
    }

    public ProductionCertificateAggregate(
        string gridArea,
        Period period,
        Technology technology,
        string meteringPointOwner,
        string gsrn,
        long quantity)
    {
        var @event = new ProductionCertificateCreated(Guid.NewGuid(), gridArea, period, technology, meteringPointOwner,
            new ShieldedValue<string>(Value: gsrn, R: BigInteger.Zero),
            new ShieldedValue<long>(Value: quantity, R: BigInteger.Zero));

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    #region Immutable Properties
    public string MeteringPointOwner { get; protected set; } = "";
    public ShieldedValue<string> GSRN { get; protected set; }
    public ShieldedValue<long> Quantity { get; protected set; }
    public string GridArea { get; protected set; }
    public Period Period { get; protected set; }
    public Technology Technology { get; protected set; }
    #endregion

    public string CertificateOwner { get; protected set; } = "";
    public IssuedState? IssuedState { get; protected set; }

    public void Issue()
    {
        if (IssuedState is not null)
            throw new InvalidOperationException(
                $"Cannot issue when certificate is already {IssuedState}"); //TODO: Exception type

        var @event = new ProductionCertificateIssued(Id, MeteringPointOwner, GSRN.Value);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    public void Reject(string reason)
    {
        if (IssuedState is not null)
            throw new InvalidOperationException(
                $"Cannot reject when certificate is already {IssuedState}"); //TODO: Exception type

        var @event = new ProductionCertificateRejected(Id, reason, MeteringPointOwner, GSRN.Value);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    public void Transfer(string from, string to)
    {
        if (string.Equals(from, to))
            throw new InvalidOperationException("Cannot transfer to the same owner"); //TODO: Exception type
        if (string.Equals(to, CertificateOwner))
            throw new InvalidOperationException($"Cannot transfer certificate to the current owner {CertificateOwner}"); //TODO: Exception type
        if (!string.Equals(from, CertificateOwner))
            throw new InvalidOperationException("Can only transfer from current owner"); //TODO: Exception type

        var @event = new CertificateTransferred(Id, from, to, GridArea, Period, Technology, GSRN, Quantity);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateCreated @event)
    {
        Id = @event.CertificateId;
        CertificateOwner = @event.MeteringPointOwner;
        MeteringPointOwner = @event.MeteringPointOwner;
        GSRN = @event.ShieldedGSRN;
        Quantity = @event.ShieldedQuantity;
        GridArea = @event.GridArea;
        Period = @event.Period;
        Technology = @event.Technology;

        Version++;
    }

    private void Apply(ProductionCertificateIssued @event)
    {
        IssuedState = GranularCertificateIssuer.IssuedState.Issued;

        Version++;
    }

    private void Apply(ProductionCertificateRejected @event)
    {
        IssuedState = GranularCertificateIssuer.IssuedState.Rejected;

        Version++;
    }

    private void Apply(CertificateTransferred @event)
    {
        CertificateOwner = @event.To;

        Version++;
    }
}

public enum IssuedState //TODO: Would like this to be private
{
    Issued,
    Rejected
}
