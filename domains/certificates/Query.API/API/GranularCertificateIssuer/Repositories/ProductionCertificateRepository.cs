using System;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents.Aggregates;
using Marten;

namespace API.GranularCertificateIssuer.Repositories;

public class ProductionCertificateRepository : IProductionCertificateRepository
{
    private readonly IDocumentStore store;

    public ProductionCertificateRepository(IDocumentStore store) => this.store = store;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
        => store.Save(productionCertificate, cancellationToken);

    public Task<ProductionCertificate?> Get(Guid id, int? version = null, CancellationToken cancellationToken = default) =>
        store.Get<ProductionCertificate>(id, version, cancellationToken);
}
