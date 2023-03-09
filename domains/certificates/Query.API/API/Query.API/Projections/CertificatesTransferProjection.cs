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

public class CertificatesTransferProjection : IProjection
{
    public Task ApplyAsync(IDocumentOperations operations, IReadOnlyList<StreamAction> streams,
        CancellationToken cancellation)
    {
        Apply(operations, streams);
        return Task.CompletedTask;
    }

    public void Apply(IDocumentOperations operations, IReadOnlyList<StreamAction> streams)
    {
        var events = streams.SelectMany(x => x.Events).OrderBy(s => s.Sequence).Select(s => s.Data);

        foreach (var @event in events)
        {
            if (@event is ProductionCertificateTransferred productionCertificateTransferred)
            {
                var source = operations.Load<CertificatesByOwnerView>(productionCertificateTransferred.Source)
                             ?? throw new CertificateDomainException(
                                 productionCertificateTransferred.CertificateId,
                                 $"Cannot transfer from {productionCertificateTransferred.Source}. {productionCertificateTransferred.Source} cannot be found"
                             );
                var cert = source.Certificates[productionCertificateTransferred.CertificateId];
                var certificateView = SetupCertificateView(cert);

                RemoveCertificateFromSource(operations, source, productionCertificateTransferred);
                AddCertificateToTarget(operations, productionCertificateTransferred, certificateView);
            }
        }
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

    private static void RemoveCertificateFromSource(IDocumentOperations operations, CertificatesByOwnerView? source,
        ProductionCertificateTransferred productionCertificateTransferred)
    {
        source.Certificates.Remove(productionCertificateTransferred.CertificateId);
        operations.Update(source);
    }

    private static CertificateView SetupCertificateView(CertificateView cert)
    {
        var certificateView = new CertificateView()
        {
            CertificateId = cert.CertificateId,
            DateFrom = cert.DateFrom,
            DateTo = cert.DateTo,
            Quantity = cert.Quantity,
            GSRN = cert.GSRN,
            GridArea = cert.GridArea,
            TechCode = cert.TechCode,
            FuelCode = cert.FuelCode,
            Status = CertificateStatus.Issued
        };
        return certificateView;
    }
}
