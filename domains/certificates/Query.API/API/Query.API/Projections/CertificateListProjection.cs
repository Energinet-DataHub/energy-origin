using System;
using System.Collections.Generic;
using CertificateEvents;
using Marten.Events.Projections;
using Marten.Schema;

namespace API.Query.API.Projections;

public class CertificateListProjection : MultiStreamAggregation<CertificateListProj, string>
{
    public CertificateListProjection()
    {
        Identity<ProductionCertificateCreated>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateIssued>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateRejected>(e => e.MeteringPointOwner);
    }

    public void Apply(ProductionCertificateCreated @event, CertificateListProj view)
    {
        view.MeteringPointOwner = @event.MeteringPointOwner;
        view.Certificates[@event.CertificateId] = new Cert
        {
            DateFrom = @event.Period.DateFrom,
            DateTo = @event.Period.DateTo,
            Quantity = @event.ShieldedQuantity.Value,
            GSRN = @event.ShieldedGSRN.Value,
            TodoStatus = 1
        };
    }

    public void Apply(ProductionCertificateIssued @event, CertificateListProj view)
    {
        view.Certificates[@event.CertificateId].TodoStatus = 2;
    }

    public void Apply(ProductionCertificateRejected @event, CertificateListProj view)
    {
        view.Certificates[@event.CertificateId].TodoStatus = 3;
    }
}

public class CertificateListProj
{
    [Identity] public string MeteringPointOwner { get; set; } = "";

    public Dictionary<Guid, Cert> Certificates { get; set; } = new();
}

public class Cert
{
    public long DateFrom { get; set; }
    public long DateTo { get; set; }
    public long Quantity { get; set; }
    public string GSRN { get; set; }
    public int TodoStatus { get; set; }
}
