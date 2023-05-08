using Contracts.Certificates;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache
{
    public interface ICertificateEventsInMemoryCache
    {
        void AddCertificateWithCommandId(CommandId commandId, ProductionCertificateCreatedEvent msg);
        ProductionCertificateCreatedEvent? PopCertificateWithCommandId(CommandId commandId);
    }
}
