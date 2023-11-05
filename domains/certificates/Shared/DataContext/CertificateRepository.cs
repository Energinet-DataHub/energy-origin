using System;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace DataContext;

public class CertificateRepository : ICertificateRepository
{
    private readonly ApplicationDbContext dbContext;

    public CertificateRepository(ApplicationDbContext dbContext) => this.dbContext = dbContext;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(productionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task Save(ConsumptionCertificate consumptionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(consumptionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ConsumptionCertificate?> GetConsumptionCertificate(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ConsumptionCertificates.FindAsync(new object?[] { id }, cancellationToken).AsTask();

    public Task<ProductionCertificate?> GetProductionCertificate(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ProductionCertificates.FindAsync(new object?[] { id }, cancellationToken).AsTask();
}
