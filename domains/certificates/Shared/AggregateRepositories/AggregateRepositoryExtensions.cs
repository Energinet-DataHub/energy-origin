using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CertificateEvents.Aggregates;
using Marten;

namespace AggregateRepositories;

public static class AggregateRepositoryExtensions
{
    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    public static async Task Save(this IDocumentStore store, AggregateBase aggregate, CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            await using var session = store.LightweightSession();

            var events = aggregate.GetUncommittedEvents().ToArray();

            session.Events.Append(aggregate.Id, aggregate.Version, events);

            await session.SaveChangesAsync(cancellationToken);

            aggregate.ClearUncommittedEvents();
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    public static async Task<T?> Get<T>(this IDocumentStore store, Guid id, int? version = null, CancellationToken cancellationToken = default) where T : AggregateBase
    {
        await using var session = store.LightweightSession();

        return await session.Events.AggregateStreamAsync<T>(id, version ?? 0, token: cancellationToken);
    }
}
