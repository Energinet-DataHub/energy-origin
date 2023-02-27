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

    public string MeteringPointOwner { get; protected set; } = ""; //TODO: Should this be Owner instead?
    public string GSRN { get; protected set; } = "";
    public IssuedState? IssuedState { get; protected set; }

    public void Issue()
    {
        if (IssuedState is not null)
        {
            throw new InvalidOperationException($"Cannot issue when certificate is already {IssuedState}"); //TODO: Exception type
        }

        var @event = new ProductionCertificateIssued(Id, MeteringPointOwner, GSRN);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    public void Reject(string reason)
    {
        if (IssuedState is not null)
        {
            throw new InvalidOperationException($"Cannot reject when certificate is already {IssuedState}"); //TODO: Exception type
        }

        var @event = new ProductionCertificateRejected(Id, reason, MeteringPointOwner, GSRN);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateCreated @event)
    {
        Id = @event.CertificateId;
        MeteringPointOwner = @event.MeteringPointOwner;
        GSRN = @event.ShieldedGSRN.Value;

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
}

public enum IssuedState //TODO: Would like this to be private
{
    Issued,
    Rejected
}
