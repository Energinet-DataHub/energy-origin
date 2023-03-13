using System;
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
using Marten.Internal.Sessions;

namespace API.Query.API.Projections;

public class CertificatesByOwnerProjection : IProjection
{
    private static CertificatesByOwnerView view;
    private static string owner;

    private bool EnableDocumentTrackingDuringRebuilds = true;

    public Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams,
        CancellationToken cancellation)
    {
        Apply(operations, streams);
        return Task.CompletedTask;
    }

    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        var events = streams
            .SelectMany(x => x.Events)
            .OrderBy(s => s.Sequence)
            .Select(s =>  s.Data);


        foreach (var @event in events)
        {
            if (@event is ProductionCertificateTransferred productionCertificateTransferred)
            {
                // ApplyProductionCertificateTransferred(operations, productionCertificateTransferred);
            }
            else if (@event is ProductionCertificateCreated productionCertificateCreated)
            {
                ApplyProductionCertificateCreated(operations, productionCertificateCreated);
            }
            else if (@event is ProductionCertificateIssued productionCertificateIssued)
            {
                ApplyProductionCertificateIssued(operations, productionCertificateIssued);
            }
            else if (@event is ProductionCertificateRejected productionCertificateRejected)
            {
                ApplyProductionCertificateRejected(operations, productionCertificateRejected);
            }
        }
    }

    private static void ApplyProductionCertificateRejected(IDocumentOperations operations,
        ProductionCertificateRejected productionCertificateRejected)
    {
        var view = operations.Load<CertificatesByOwnerView>(owner)
                   ?? throw new CertificateDomainException(
                       productionCertificateRejected.CertificateId,
                       $"Cannot reject {productionCertificateRejected.CertificateId}. {productionCertificateRejected.CertificateId} cannot be found"
                   );
        view.Certificates[productionCertificateRejected.CertificateId].Status = CertificateStatus.Rejected;
        operations.Update(view);
    }

    private static void ApplyProductionCertificateIssued(IDocumentOperations operations,
        ProductionCertificateIssued productionCertificateIssued)
    {
        // var view = operations.Load<CertificatesByOwnerView>(owner)
        //            ?? throw new CertificateDomainException(
        //                productionCertificateIssued.CertificateId,
        //                $"Cannot reject {productionCertificateIssued.CertificateId}. {productionCertificateIssued.CertificateId} cannot be found"
        //            );

        view.Certificates[productionCertificateIssued.CertificateId].Status = CertificateStatus.Issued;
        operations.Update(view);
    }

    private static void ApplyProductionCertificateCreated(IDocumentOperations operations,
        ProductionCertificateCreated productionCertificateCreated)
    {
        owner = productionCertificateCreated.MeteringPointOwner;
        view = new CertificatesByOwnerView
        {
            Owner = owner,
            Certificates =
            {
                [productionCertificateCreated.CertificateId] = new CertificateView
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
                }
            }
        };
        operations.Store(view);
    }

    private static void ApplyProductionCertificateTransferred(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        var source = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source)
                     ?? throw new CertificateDomainException(
                         productionCertificateTransferred.CertificateId,
                         $"Cannot transfer from {productionCertificateTransferred.Source}. {productionCertificateTransferred.Source} cannot be found"
                     );
        var cert = source.Certificates[productionCertificateTransferred.CertificateId];

        RemoveCertificateFromSource(operations, source, productionCertificateTransferred);
        AddCertificateToTarget(operations, productionCertificateTransferred, cert);
    }

    private static void AddCertificateToTarget(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred, CertificateView certificateView)
    {
        var target = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Target);

        if (target == null)
        {
            target = new CertificatesByOwnerView
            {
                Owner = productionCertificateTransferred.Target
            };
            target.Certificates.Add(certificateView.CertificateId, certificateView);
            operations.Store(target);
        }
        else
        {
            target.Certificates.Add(certificateView.CertificateId, certificateView);
            operations.Update(target);
        }
    }

    private static void RemoveCertificateFromSource(IDocumentOperations operations, CertificatesByOwnerView source,
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        source.Certificates.Remove(productionCertificateTransferred.CertificateId);
        operations.Update(source);
    }
}
