using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.Projections.Exceptions;
using API.Query.API.Projections.Views;
using CertificateEvents;
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
        var views = new HashSet<CertificatesByOwnerView>();
        var duplicatedStream = streams
            .SelectMany(x => x.Events)
            .DistinctBy(x => x.StreamId);

        if (duplicatedStream.Count() != 1)
            throw new ProjectionException(Guid.Empty, "Only one certificate is allowed in the stream");

        var events = streams
            .SelectMany(x => x.Events)
            .OrderBy(s => s.Sequence)
            .Select(s => s.Data);

        foreach (var @event in events)
        {
            switch (@event)
            {
                case ProductionCertificateCreated productionCertificateCreated:
                    CreateCertificatesByOwnerView(operations,
                        productionCertificateCreated, views);
                    break;
                case ProductionCertificateIssued productionCertificateIssued:
                    ApplyProductionCertificateIssued(operations,
                        productionCertificateIssued, views);
                    break;
                case ProductionCertificateRejected productionCertificateRejected:
                    ApplyProductionCertificateRejected(
                        operations, productionCertificateRejected, views);
                    break;
                case ProductionCertificateTransferred productionCertificateTransferred:
                    ApplyProductionCertificateTransferred(operations, productionCertificateTransferred, views);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        operations.Store(views.ToArray());
    }

    private static void ApplyProductionCertificateRejected(IDocumentOperations operations,
        ProductionCertificateRejected productionCertificateRejected, ISet<CertificatesByOwnerView> views)
    {
        var view = views
                       .FirstOrDefault(it =>
                           it.Certificates.ContainsKey(productionCertificateRejected.CertificateId)) ??
                   GetCertificatesByOwnerView(operations, productionCertificateRejected.CertificateId);

        view.Certificates[productionCertificateRejected.CertificateId].Status = CertificateStatus.Rejected;
        views.Add(view);
    }

    private static void ApplyProductionCertificateIssued(IDocumentOperations operations,
        ProductionCertificateIssued productionCertificateIssued, ISet<CertificatesByOwnerView> views)
    {
        var view = views
                       .FirstOrDefault(it =>
                           it.Certificates.ContainsKey(productionCertificateIssued.CertificateId)) ??
                   GetCertificatesByOwnerView(operations, productionCertificateIssued.CertificateId);

        view.Certificates[productionCertificateIssued.CertificateId].Status = CertificateStatus.Issued;
        views.Add(view);
    }

    private static void CreateCertificatesByOwnerView(IDocumentOperations operations,
        ProductionCertificateCreated productionCertificateCreated, ISet<CertificatesByOwnerView> views)
    {
        var view = operations.Load<CertificatesByOwnerView>(productionCertificateCreated.MeteringPointOwner);
        var certificateView = new CertificateView
        {
            CertificateId = productionCertificateCreated.CertificateId,
            DateFrom = productionCertificateCreated.Period.DateFrom,
            DateTo = productionCertificateCreated.Period.DateTo,
            Quantity = productionCertificateCreated.ShieldedQuantity.Shielded,
            GSRN = productionCertificateCreated.ShieldedGSRN.Shielded,
            GridArea = productionCertificateCreated.GridArea,
            TechCode = productionCertificateCreated.Technology.TechCode,
            FuelCode = productionCertificateCreated.Technology.FuelCode,
            Status = CertificateStatus.Creating
        };

        if (view == null)
        {
            view = new CertificatesByOwnerView
            {
                Owner = productionCertificateCreated.MeteringPointOwner,
                Certificates =
                {
                    [productionCertificateCreated.CertificateId] = certificateView
                }
            };
        }
        else
        {
            view.Certificates.Add(productionCertificateCreated.CertificateId, certificateView);
        }

        views.Add(view);
    }

    private static void ApplyProductionCertificateTransferred(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred, ISet<CertificatesByOwnerView> views)
    {
        var source = views
                         .FirstOrDefault(it =>
                             it.Certificates.ContainsKey(productionCertificateTransferred.CertificateId))
                     ?? operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source)
                     ?? throw new ProjectionException(
                         productionCertificateTransferred.CertificateId,
                         $"Cannot transfer from {productionCertificateTransferred.Source}. View for {productionCertificateTransferred.Source} cannot be found"
                     );
        var cert = source.Certificates[productionCertificateTransferred.CertificateId];

        RemoveCertificateFromSource(source, productionCertificateTransferred, views);
        AddCertificateToTarget(operations, productionCertificateTransferred, cert, views);
    }

    private static void AddCertificateToTarget(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred, CertificateView certificateView,
        ISet<CertificatesByOwnerView> views)
    {
        var view = views
                       .FirstOrDefault(it =>
                           it.Certificates.ContainsKey(productionCertificateTransferred.CertificateId))
                   ?? operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Target);

        if (view == null)
        {
            view = new CertificatesByOwnerView
            {
                Owner = productionCertificateTransferred.Target,
                Certificates =
                {
                    [certificateView.CertificateId] = certificateView
                }
            };
        }
        else
        {
            view.Certificates.Add(certificateView.CertificateId, certificateView);
        }

        views.Add(view);
    }

    private static void RemoveCertificateFromSource(CertificatesByOwnerView source,
        ProductionCertificateTransferred productionCertificateTransferred, ISet<CertificatesByOwnerView> views)
    {
        source.Certificates.Remove(productionCertificateTransferred.CertificateId);
        views.Add(source);
    }

    private static CertificatesByOwnerView GetCertificatesByOwnerView(IDocumentOperations operations,
        Guid certificateId)
    {
        var owner = operations.Events.QueryRawEventDataOnly<ProductionCertificateCreated>()
            .First(e => e.CertificateId == certificateId).MeteringPointOwner;

        var view = operations.Load<CertificatesByOwnerView>(owner) ?? throw new ProjectionException(
            certificateId,
            $"Cannot find CertificatesByOwnerView for certificate {certificateId}."
        );
        return view;
    }
}
