using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents.Aggregates;
using Marten;

namespace API.GranularCertificateIssuer.Repositories;

public static class AggregateRepositoryExtensions
{
    public static async Task Save(this IDocumentStore store, AggregateBase aggregate, CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession();
        // Take non-persisted events, push them to the event stream, indexed by the aggregate ID
        var events = aggregate.GetUncommittedEvents().ToArray();
        session.Events.Append(aggregate.Id, aggregate.Version, events);
        await session.SaveChangesAsync(cancellationToken);
        // Once successfully persisted, clear events from list of uncommitted events
        aggregate.ClearUncommittedEvents();
    }

    public static async Task<T?> Get<T>(this IDocumentStore store, Guid id, int? version = null, CancellationToken cancellationToken = default) where T : AggregateBase
    {
        await using var session = store.LightweightSession();
        return await session.Events.AggregateStreamAsync<T>(id, version ?? 0, token: cancellationToken);
    }
}
