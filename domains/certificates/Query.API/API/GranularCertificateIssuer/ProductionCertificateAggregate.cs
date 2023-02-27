using System;
using System.Numerics;
using CertificateEvents;
using CertificateEvents.Primitives;

namespace API.GranularCertificateIssuer;

public class ProductionCertificateAggregate : AggregateBase
{
    public string CertificateOwner { get; protected set; } = "";
    private IssuedState? issuedState;

    // Fields for the immutable properties of the certificate
    private string meteringPointOwner = "";
    private ShieldedValue<string> GSRN = new("", BigInteger.Zero);
    private ShieldedValue<long> quantity = new(0, BigInteger.Zero);
    private string gridArea = "";
    private Period period = new(1, 1);
    private Technology technology = new("", "");

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
    private void Apply(ProductionCertificateCreated @event)
    {
        Id = @event.CertificateId;
        CertificateOwner = @event.MeteringPointOwner;
        meteringPointOwner = @event.MeteringPointOwner;
        GSRN = @event.ShieldedGSRN;
        quantity = @event.ShieldedQuantity;
        gridArea = @event.GridArea;
        period = @event.Period;
        technology = @event.Technology;

        Version++;
    }

    public void Issue()
    {
        if (issuedState is not null)
            throw new InvalidOperationException(
                $"Cannot issue when certificate is already {issuedState}"); //TODO: Exception type

        var @event = new ProductionCertificateIssued(Id, meteringPointOwner, GSRN.Value);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateIssued @event)
    {
        issuedState = IssuedState.Issued;

        Version++;
    }

    public void Reject(string reason)
    {
        if (issuedState is not null)
            throw new InvalidOperationException(
                $"Cannot reject when certificate is already {issuedState}"); //TODO: Exception type

        var @event = new ProductionCertificateRejected(Id, reason, meteringPointOwner, GSRN.Value);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateRejected @event)
    {
        issuedState = IssuedState.Rejected;

        Version++;
    }

    public void Transfer(string from, string to)
    {
        if (string.Equals(from, to))
            throw new InvalidOperationException("Cannot transfer to the same owner"); //TODO: Exception type
        if (issuedState != IssuedState.Issued)
            throw new InvalidOperationException("Transfer only allowed on issued certificates");//TODO: Exception type
        if (string.Equals(to, CertificateOwner))
            throw new InvalidOperationException($"Cannot transfer certificate to the current owner {CertificateOwner}"); //TODO: Exception type
        if (!string.Equals(from, CertificateOwner))
            throw new InvalidOperationException("Can only transfer from current owner"); //TODO: Exception type

        var @event = new CertificateTransferred(Id, from, to, gridArea, period, technology, GSRN, quantity);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(CertificateTransferred @event)
    {
        CertificateOwner = @event.To;

        Version++;
    }

    private enum IssuedState
    {
        Issued,
        Rejected
    }
}
