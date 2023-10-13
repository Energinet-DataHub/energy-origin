using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Data;

public interface IConsumptionCertificateRepository
{
    Task Save(ConsumptionCertificate consumptionCertificate, CancellationToken cancellationToken = default);
    Task<ConsumptionCertificate?> Get(Guid id, CancellationToken cancellationToken = default);
}
