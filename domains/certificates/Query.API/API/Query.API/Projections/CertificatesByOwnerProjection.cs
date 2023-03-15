using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.Projections.Views;
using CertificateEvents;
using CertificateEvents.Exceptions;
using Marten;
using Marten.Events;
using Marten.Events.Projections;

namespace API.Query.API.Projections;

public class CertificatesByOwnerProjection : IProjection
{
    public Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams,
        CancellationToken cancellation)
    {
        Apply(operations, streams);
        return Task.CompletedTask;
    }

    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        CertificatesByOwnerView view = null;

        var events = streams
            .SelectMany(x => x.Events)
            .OrderBy(s => s.Sequence)
            .Select(s => s.Data);

        foreach (var @event in events)
        {
            if (@event is ProductionCertificateCreated productionCertificateCreated)
            {
                view = CreateCertificatesByOwnerView(operations, productionCertificateCreated);
            }
            else if (@event is ProductionCertificateIssued productionCertificateIssued)
            {
                view = ApplyProductionCertificateIssued(productionCertificateIssued, view);
            }
            else if (@event is ProductionCertificateRejected productionCertificateRejected)
            {
                view = ApplyProductionCertificateRejected(productionCertificateRejected, view);
            }
            else if (@event is ProductionCertificateTransferred productionCertificateTransferred)
            {
                view = ApplyProductionCertificateTransferred(operations, productionCertificateTransferred);
            }
        }

        operations.Store(view);
    }

    private static CertificatesByOwnerView ApplyProductionCertificateRejected(
        ProductionCertificateRejected productionCertificateRejected, CertificatesByOwnerView? view)
    {
        view.Certificates[productionCertificateRejected.CertificateId].Status = CertificateStatus.Rejected;
        return view;
    }

    private static CertificatesByOwnerView ApplyProductionCertificateIssued(
        ProductionCertificateIssued productionCertificateIssued, CertificatesByOwnerView view)
    {
        view.Certificates[productionCertificateIssued.CertificateId].Status = CertificateStatus.Issued;
        return view;
    }

    private static CertificatesByOwnerView CreateCertificatesByOwnerView(IDocumentOperations operations,
        ProductionCertificateCreated productionCertificateCreated)
    {
        var view = operations.Load<CertificatesByOwnerView>(productionCertificateCreated.MeteringPointOwner);
        var certificateView = new CertificateView
        {
            CertificateId = productionCertificateCreated.CertificateId,
            DateFrom = productionCertificateCreated.Period.DateFrom,
            DateTo = productionCertificateCreated.Period.DateTo,
            Quantity = productionCertificateCreated.ShieldedQuantity.Value,
            GSRN = productionCertificateCreated.ShieldedGSRN.Value,
            GridArea = productionCertificateCreated.GridArea,
            TechCode = productionCertificateCreated.Technology.TechCode,
            FuelCode = productionCertificateCreated.Technology.FuelCode,
            Status = CertificateStatus.Creating
        };

        if (view == null)
        {
            return new CertificatesByOwnerView
            {
                Owner = productionCertificateCreated.MeteringPointOwner,
                Certificates =
                {
                    [productionCertificateCreated.CertificateId] = certificateView
                }
            };
        }

        view.Certificates.Add(productionCertificateCreated.CertificateId, certificateView);
        return view;
    }

    private static CertificatesByOwnerView ApplyProductionCertificateTransferred(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        var source = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source)
                     ?? throw new CertificateDomainException(
                         productionCertificateTransferred.CertificateId,
                         $"Cannot transfer from {productionCertificateTransferred.Source}. {productionCertificateTransferred.Source} cannot be found"
                     );
        var cert = source.Certificates[productionCertificateTransferred.CertificateId];

        RemoveCertificateFromSource(operations, source, productionCertificateTransferred);
        return AddCertificateToTarget(operations, productionCertificateTransferred, cert);
    }

    private static CertificatesByOwnerView AddCertificateToTarget(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred, CertificateView certificateView)
    {
        var view = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Target);

        if (view == null)
        {
            return new CertificatesByOwnerView
            {
                Owner = productionCertificateTransferred.Target,
                Certificates =
                {
                    [certificateView.CertificateId] = certificateView
                }
            };
        }

        view.Certificates.Add(certificateView.CertificateId, certificateView);
        return view;
    }

    private static void RemoveCertificateFromSource(IDocumentOperations operations, CertificatesByOwnerView source,
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        source.Certificates.Remove(productionCertificateTransferred.CertificateId);
        operations.Update(source);
    }
}
