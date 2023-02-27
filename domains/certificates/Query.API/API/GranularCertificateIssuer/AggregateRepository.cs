using Marten;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace API.GranularCertificateIssuer;

public sealed class AggregateRepository
{
    private readonly IDocumentStore store;

    public AggregateRepository(IDocumentStore store) => this.store = store;

    public async Task Save(AggregateBase aggregate, CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();
        // Take non-persisted events, push them to the event stream, indexed by the aggregate ID
        var events = aggregate.GetUncommittedEvents().ToArray();
        session.Events.Append(aggregate.Id, aggregate.Version, events);
        await session.SaveChangesAsync(cancellationToken);
        // Once successfully persisted, clear events from list of uncommitted events
        aggregate.ClearUncommittedEvents();
    }

    public async Task<T> Get<T>(Guid id, int? version = null, CancellationToken cancellationToken = default) where T : AggregateBase
    {
        await using var session = store.LightweightSession();
        var aggregate = await session.Events.AggregateStreamAsync<T>(id, version ?? 0, token: cancellationToken);
        return aggregate ?? throw new InvalidOperationException($"No aggregate by id {id}.");
    }
}

public interface IProductionCertificateRepository
{
    Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default);
    Task<ProductionCertificate> Get(Guid id, int? version = null, CancellationToken cancellationToken = default);
}

public class ProductionCertificateRepository : IProductionCertificateRepository
{
    private readonly AggregateRepository aggregateRepository;

    public ProductionCertificateRepository(AggregateRepository aggregateRepository) => this.aggregateRepository = aggregateRepository;

    public Task Save(ProductionCertificate productionCertificate, CancellationToken cancellationToken = default)
        => aggregateRepository.Save(productionCertificate, cancellationToken);

    public Task<ProductionCertificate> Get(Guid id, int? version = null, CancellationToken cancellationToken = default) =>
        aggregateRepository.Get<ProductionCertificate>(id, version, cancellationToken);
}
