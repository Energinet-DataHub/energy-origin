using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using CertificateEvents;
using Marten;
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
            TodoStatus = CertificateStatus.Creating
        };
    }

    public void Apply(ProductionCertificateIssued @event, CertificateListProj view)
    {
        view.Certificates[@event.CertificateId].TodoStatus = CertificateStatus.Issued;
    }

    public void Apply(ProductionCertificateRejected @event, CertificateListProj view)
    {
        view.Certificates[@event.CertificateId].TodoStatus = CertificateStatus.Rejected;
    }

    // TODO: Maybe there should be a delete part in some of the Apply()-methods, so old certificates are pruned
}

public class CertificateListProj
{
    [Identity] public string MeteringPointOwner { get; set; } = "";

    public Dictionary<Guid, Cert> Certificates { get; set; } = new();

    public CertificateList ToApiModel()
    {
        var certificates = Certificates.Values
            .Select(c => new Certificate
            {
                GSRN = c.GSRN,
                DateFrom = c.DateFrom,
                DateTo = c.DateTo,
                Quantity = c.Quantity
            });

        return new CertificateList
        {
            Result = certificates
                .OrderByDescending(c => c.DateFrom)
                .ThenBy(c => c.GSRN)
                .ToArray()
        };
    }
}

public class Cert
{
    public long DateFrom { get; set; }
    public long DateTo { get; set; }
    public long Quantity { get; set; }
    public string GSRN { get; set; }
    public CertificateStatus TodoStatus { get; set; }
}

public enum CertificateStatus
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
};
