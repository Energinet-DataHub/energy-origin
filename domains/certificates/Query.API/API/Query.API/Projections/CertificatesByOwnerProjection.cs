using System;
using System.Collections.Generic;
using System.Linq;
using API.Query.API.ApiModels.Responses;
using CertificateEvents;
using Marten.Events.Projections;
using Marten.Schema;

namespace API.Query.API.Projections;

public class CertificatesByOwnerProjection : MultiStreamAggregation<CertificatesByOwnerView, string>
{
    public CertificatesByOwnerProjection()
    {
        Identity<ProductionCertificateCreated>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateIssued>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateRejected>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateTransferred>(e => e.From);
        Identity<ProductionCertificateTransferred>(e => e.To);
    }

    public void Apply(ProductionCertificateTransferred @event, CertificatesByOwnerView view)
    {
        if (view.Owner == @event.From)
        {
            view.Certificates.Remove(@event.CertificateId);
        }
        else
        {
            view.Certificates[@event.CertificateId] = new CertificateView
            {
                CertificateId = @event.CertificateId,
                DateFrom = @event.Period.DateFrom,
                DateTo = @event.Period.DateTo,
                Quantity = @event.ShieldedQuantity.Value,
                GSRN = @event.ShieldedGSRN.Value,
                GridArea = @event.GridArea,
                TechCode = @event.Technology.TechCode,
                FuelCode = @event.Technology.FuelCode,
                Status = CertificateStatus.Issued // TODO: What to do
            };
        }
    }

    public void Apply(ProductionCertificateCreated @event, CertificatesByOwnerView view)
    {
        view.Owner = @event.MeteringPointOwner;
        view.Certificates[@event.CertificateId] = new CertificateView
        {
            CertificateId = @event.CertificateId,
            DateFrom = @event.Period.DateFrom,
            DateTo = @event.Period.DateTo,
            Quantity = @event.ShieldedQuantity.Value,
            GSRN = @event.ShieldedGSRN.Value,
            GridArea = @event.GridArea,
            TechCode = @event.Technology.TechCode,
            FuelCode = @event.Technology.FuelCode,
            Status = CertificateStatus.Creating
        };
    }

    public void Apply(ProductionCertificateIssued @event, CertificatesByOwnerView view)
        => view.Certificates[@event.CertificateId].Status = CertificateStatus.Issued;

    public void Apply(ProductionCertificateRejected @event, CertificatesByOwnerView view)
        => view.Certificates[@event.CertificateId].Status = CertificateStatus.Rejected;
}

public class CertificatesByOwnerView
{
    [Identity]
    public string Owner { get; set; } = "";

    public Dictionary<Guid, CertificateView> Certificates { get; set; } = new();

    public CertificateList ToApiModel()
    {
        var certificates = Certificates.Values
            .Select(c => new Certificate
            {
                Id = c.CertificateId,
                GSRN = c.GSRN,
                GridArea = c.GridArea,
                DateFrom = c.DateFrom,
                DateTo = c.DateTo,
                Quantity = c.Quantity,
                TechCode = c.TechCode,
                FuelCode = c.FuelCode
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

public class CertificateView
{
    public Guid CertificateId { get; set; }
    public long DateFrom { get; set; }
    public long DateTo { get; set; }
    public long Quantity { get; set; }
    public string GSRN { get; set; } = "";
    public string GridArea { get; set; } = "";
    public string TechCode { get; set; } = "";
    public string FuelCode { get; set; } = "";
    public CertificateStatus Status { get; set; }
}

public enum CertificateStatus
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
};
