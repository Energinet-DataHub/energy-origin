using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataContext;

public interface ICertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate?> GetProductionCertificate(Guid id, CancellationToken cancellationToken = default);
    Task Save(ConsumptionCertificate consumptionCertificate, CancellationToken cancellationToken = default);
    Task<ConsumptionCertificate?> GetConsumptionCertificate(Guid id, CancellationToken cancellationToken = default);
}
