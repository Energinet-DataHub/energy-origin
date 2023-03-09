using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Responses;
using CertificateEvents;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Schema;

namespace API.Query.API.Projections;


public class CertificatesTransferProjection : IProjection
{
    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        var events = streams.SelectMany(x => x.Events).OrderBy(s => s.Sequence).Select(s => s.Data);

        foreach (var @event in events)
        {
            if (@event is ProductionCertificateTransferred productionCertificateTransferred)
            {
                var source = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source);
                var cert = source.Certificates[productionCertificateTransferred.CertificateId];
                source.Certificates.Remove(productionCertificateTransferred.CertificateId);
                operations.Update(source);

                var target = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Target);
                var certificateView = new CertificateView()
                {
                    CertificateId = cert.CertificateId,
                    DateFrom = cert.DateFrom,
                    DateTo = cert.DateTo,
                    Quantity = cert.Quantity,
                    GSRN = cert.GSRN,
                    GridArea = cert.GridArea + "TESTERS",
                    TechCode = cert.TechCode,
                    FuelCode = cert.FuelCode,
                    Status = CertificateStatus.Issued
                };

                if (target == null)
                {
                    target = new CertificatesByOwnerView();
                    target.Owner = productionCertificateTransferred.Target;
                    target.Certificates.Add(cert.CertificateId, certificateView);
                    operations.Store(target);
                }
                else
                {
                    target.Certificates.Add(cert.CertificateId, certificateView);
                    operations.Update(target);
                }
            }
        }
    }

    public Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams,
        CancellationToken cancellation)
    {
        Apply(operations, streams);
        return Task.CompletedTask;
    }
}



public class CertificatesByOwnerProjection : MultiStreamAggregation<CertificatesByOwnerView, string>
{
    public CertificatesByOwnerProjection()
    {
        Identity<ProductionCertificateCreated>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateIssued>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateRejected>(e => e.MeteringPointOwner);
        // Identity<ProductionCertificateTransferred>(e => e.Source);
        // Identity<ProductionCertificateTransferred>(e => e.Target);
        //CustomGrouping(new TransferGroup());
    }

    // public void Apply(IEvent<ProductionCertificateTransferred> @event, CertificatesByOwnerView view)
    // {
    //     var meta = @event.Headers.Get("foo");
    //

    // }

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
    [Identity] public string Owner { get; set; } = "";

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
