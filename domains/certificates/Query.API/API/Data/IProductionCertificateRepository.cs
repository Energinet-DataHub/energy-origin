using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Data;

public interface IProductionCertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate?> Get(Guid id, CancellationToken cancellationToken = default);
}