using Contracts.Certificates;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Cache;

public interface ICertificateEventsInMemoryCache
{
    void AddCertificateWithCommandId(CommandId commandId, MessageWrapper<ProductionCertificateCreatedEvent> msg);
    MessageWrapper<ProductionCertificateCreatedEvent>? PopCertificateWithCommandId(CommandId commandId);
}