using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents.Aggregates;
using Marten;
using Marten.Services;

namespace AggregateRepositories;

public static class AggregateRepositoryExtensions
{
    public static async Task Save(this IDocumentStore store, AggregateBase aggregate, CancellationToken cancellationToken = default)
    {
        await using var session = store.LightweightSession(IsolationLevel.Serializable);

        var events = aggregate.GetUncommittedEvents().ToArray();

        session.Events.Append(aggregate.Id, aggregate.Version, events);

        await session.SaveChangesAsync(cancellationToken);

        aggregate.ClearUncommittedEvents();
    }

    public static async Task<T?> Get<T>(this IDocumentStore store, Guid id, int? version = null, CancellationToken cancellationToken = default) where T : AggregateBase
    {
        await using var session = store.LightweightSession(IsolationLevel.Serializable);

        return await session.Events.AggregateStreamAsync<T>(id, version ?? 0, token: cancellationToken);
    }
}
