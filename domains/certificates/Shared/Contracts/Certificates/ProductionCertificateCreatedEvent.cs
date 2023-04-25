using System;
using Domain;
using Domain.Certificates.Primitives;

namespace Contracts.Certificates
{
    public record class ProductionCertificateCreatedEvent(
        Guid CertificateId,
        string GridArea,
        Period Period,
        Technology Technology,
        string MeteringPointOwner,
        ShieldedValue<Gsrn> ShieldedGsrn,
        ShieldedValue<long> ShieldedQuantity
    );
}
