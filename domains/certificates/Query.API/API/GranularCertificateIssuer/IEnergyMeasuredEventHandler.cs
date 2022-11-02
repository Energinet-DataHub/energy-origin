using System.Threading.Tasks;
using CertificateEvents;

namespace API.GranularCertificateIssuer;

public interface IEnergyMeasuredEventHandler
{
    Task<ProductionCertificateCreated?> Handle(EnergyMeasured @event);
}
