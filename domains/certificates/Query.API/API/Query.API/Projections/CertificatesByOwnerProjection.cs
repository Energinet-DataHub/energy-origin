using API.Query.API.Projections.Views;
using CertificateEvents;
using Marten.Events.Projections;

namespace API.Query.API.Projections;

public class CertificatesByOwnerProjection : MultiStreamAggregation<CertificatesByOwnerView, string>
{
    public CertificatesByOwnerProjection()
    {
        Identity<ProductionCertificateCreated>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateIssued>(e => e.MeteringPointOwner);
        Identity<ProductionCertificateRejected>(e => e.MeteringPointOwner);
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


