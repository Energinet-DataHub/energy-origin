using System;
using System.Numerics;
using CertificateEvents.Exceptions;
using CertificateValueObjects;

namespace CertificateEvents.Aggregates;

public class ProductionCertificate : AggregateBase
{
    public string CertificateOwner { get; protected set; } = "";

    public bool IsIssued => issuedState == IssuedState.Issued;
    public bool IsRejected => issuedState == IssuedState.Rejected;

    private IssuedState? issuedState;

    // Default constructor used for loading the aggregate
    private ProductionCertificate()
    {
    }

    public ProductionCertificate(
        string gridArea,
        Period period,
        Technology technology,
        string meteringPointOwner,
        string gsrn,
        long quantity)
    {
        var certificateId = Guid.NewGuid();

        var @event = new ProductionCertificateCreated(
            certificateId,
            gridArea,
            period,
            technology,
            meteringPointOwner,
            new ShieldedValue<string>(Value: gsrn, R: BigInteger.Zero),
            new ShieldedValue<long>(Value: quantity, R: BigInteger.Zero));

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateCreated @event)
    {
        Id = @event.CertificateId;
        CertificateOwner = @event.MeteringPointOwner;

        Version++;
    }

    public void Issue()
    {
        if (issuedState is not null)
            throw new CertificateDomainException(Id, $"Cannot issue when certificate is already {issuedState.ToString()!.ToLower()}");

        var @event = new ProductionCertificateIssued(Id);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateIssued _)
    {
        issuedState = IssuedState.Issued;

        Version++;
    }

    public void Reject(string reason)
    {
        if (issuedState is not null)
            throw new CertificateDomainException(Id, $"Cannot reject when certificate is already {issuedState.ToString()!.ToLower()}");

        var @event = new ProductionCertificateRejected(Id, reason);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateRejected _)
    {
        issuedState = IssuedState.Rejected;

        Version++;
    }

    public void Transfer(string source, string target)
    {
        if (issuedState != IssuedState.Issued)
            throw new CertificateDomainException(Id, "Transfer only allowed on issued certificates");
        if (string.Equals(target, CertificateOwner))
            throw new CertificateDomainException(Id, $"Cannot transfer certificate to the current owner {target}");
        if (!string.Equals(source, CertificateOwner))
            throw new CertificateDomainException(Id, $"Cannot transfer from {source}. {source} is not current owner");

        var @event = new ProductionCertificateTransferred(Id, source, target);

        Apply(@event);
        AddUncommittedEvent(@event);
    }

    private void Apply(ProductionCertificateTransferred @event)
    {
        CertificateOwner = @event.Target;

        Version++;
    }

    private enum IssuedState
    {
        Issued,
        Rejected
    }
}
