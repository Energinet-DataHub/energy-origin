using System.Threading.Tasks;
using CertificateEvents;

namespace Issuer.Worker.GranularCertificateIssuer;

public interface IEnergyMeasuredEventHandler
{
    Task<ProductionCertificateCreated?> Handle(EnergyMeasured @event);
}
