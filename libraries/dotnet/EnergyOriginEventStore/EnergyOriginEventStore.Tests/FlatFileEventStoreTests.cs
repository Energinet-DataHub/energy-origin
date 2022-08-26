using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.FlatFile;
using EnergyOriginEventStore.Tests.Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class FlatFileEventStoreTests : EventStoreTests, IDisposable
{
    public override Task<IEventStore> buildStore() => Task.FromResult(new FlatFileEventStore() as IEventStore);

    public override bool canPersist()
    {
        return true;
    }

    public void Dispose()
    {
        Directory.Delete("store", true);
    }
}
