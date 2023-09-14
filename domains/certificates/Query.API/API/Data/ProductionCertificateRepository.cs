using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Data;

public class ProductionCertificateRepository : IProductionCertificateRepository
{
    private readonly ApplicationDbContext dbContext;

    public ProductionCertificateRepository(ApplicationDbContext dbContext) => this.dbContext = dbContext;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
    {
        dbContext.Update(productionCertificate);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ProductionCertificate?> Get(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ProductionCertificates.FindAsync(new object?[] { id }, cancellationToken).AsTask();
}
