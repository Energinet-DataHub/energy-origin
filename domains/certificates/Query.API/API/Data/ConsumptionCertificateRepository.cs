using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Data;

public class ConsumptionCertificateRepository : IConsumptionCertificateRepository
{
    private readonly ApplicationDbContext dbContext;

    public ConsumptionCertificateRepository(ApplicationDbContext dbContext) => this.dbContext = dbContext;

    public Task Save(ConsumptionCertificate consumptionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(consumptionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ConsumptionCertificate?> Get(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ConsumptionCertificates.FindAsync(new object?[] { id }, cancellationToken).AsTask();
}
