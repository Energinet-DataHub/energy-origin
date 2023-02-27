using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.GranularCertificateIssuer;

public interface IProductionCertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate> Get(Guid id, int? version = null, CancellationToken cancellationToken = default);
}
