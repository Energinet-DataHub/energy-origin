using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using EnergyOriginEventStore.Tests.Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class MemoryEventStoreTests : EventStoreTests
{
    public override Task<IEventStore> buildStore() => Task.FromResult(new MemoryEventStore() as IEventStore);

    public override bool canPersist()
    {
        return false;
    }
}
