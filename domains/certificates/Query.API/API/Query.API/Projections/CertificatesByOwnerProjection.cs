using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.Projections.Exceptions;
using API.Query.API.Projections.Views;
using Baseline;
using CertificateEvents;
using Marten;
using Marten.Events;
using Marten.Events.Projections;

namespace API.Query.API.Projections;

public class CertificatesByOwnerProjection : IProjection
{
    private static readonly List<CertificatesByOwnerView> views = new();

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
            .Select(s => s.Data);

        foreach (var @event in events)
        {
            switch (@event)
            {
                case ProductionCertificateCreated productionCertificateCreated:
                    CreateCertificatesByOwnerView(operations,
                        productionCertificateCreated);
                    break;
                case ProductionCertificateIssued productionCertificateIssued:
                    ApplyProductionCertificateIssued(operations,
                        productionCertificateIssued);
                    break;
                case ProductionCertificateRejected productionCertificateRejected:
                    ApplyProductionCertificateRejected(
                        operations, productionCertificateRejected);
                    break;
                case ProductionCertificateTransferred productionCertificateTransferred:
                    ApplyProductionCertificateTransferred(operations, productionCertificateTransferred);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        operations.Store(views.ToArray());
    }

    private static void ApplyProductionCertificateRejected(IDocumentOperations operations,
        ProductionCertificateRejected productionCertificateRejected)
    {
        var view = views
                       .FirstOrDefault(it =>
                           it.Certificates.ContainsKey(productionCertificateRejected.CertificateId)) ??
                   GetCertificatesByOwnerView(operations, productionCertificateRejected.CertificateId);

        view.Certificates[productionCertificateRejected.CertificateId].Status = CertificateStatus.Rejected;
        views.Add(view);
    }

    private static void ApplyProductionCertificateIssued(IDocumentOperations operations,
        ProductionCertificateIssued productionCertificateIssued)
    {
        var view = views
                       .FirstOrDefault(it =>
                           it.Certificates.ContainsKey(productionCertificateIssued.CertificateId)) ??
                   GetCertificatesByOwnerView(operations, productionCertificateIssued.CertificateId);

        view.Certificates[productionCertificateIssued.CertificateId].Status = CertificateStatus.Issued;
        views.Add(view);
    }

    private static void CreateCertificatesByOwnerView(IDocumentOperations operations,
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
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        var source = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source)
                     ?? throw new ProjectionException(
                         productionCertificateTransferred.CertificateId,
                         $"Cannot transfer from {productionCertificateTransferred.Source}. {productionCertificateTransferred.CertificateId} cannot be found"
                     );
        var cert = source.Certificates[productionCertificateTransferred.CertificateId];

        RemoveCertificateFromSource(source, productionCertificateTransferred);
        AddCertificateToTarget(operations, productionCertificateTransferred, cert);
    }

    private static void AddCertificateToTarget(IDocumentOperations operations,
        ProductionCertificateTransferred productionCertificateTransferred, CertificateView certificateView)
    {
        var view = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Target);

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
        ProductionCertificateTransferred productionCertificateTransferred)
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
