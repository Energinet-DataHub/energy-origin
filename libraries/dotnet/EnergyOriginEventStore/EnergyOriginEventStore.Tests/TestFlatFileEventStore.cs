using EnergyOriginEventStore.Tests.Topics;
using EventStore;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class TestFlatFileEventStore
{
    [Fact]
    public void Test_EventStoreWorks()
    {
        try
        {
            Task.Run(async () =>
            {
                var eventStore = EventStoreFactory<Said>.create();

                var message = new Said("Anton Actor", "I like to act!");

                eventStore.Produce(message, new List<String> { "Gossip", "Tabloid" });

                var consumer = eventStore.MakeConsumer<Said>("Gossip");
                var saidEvent = await consumer.Consume();

                Assert.Equal(message.Actor, saidEvent.Actor);
                Assert.Equal(message.Statement, saidEvent.Statement);
            }).Wait();
        }
        catch (Exception)
        {
            Directory.Delete("store", true);
        }
    }
}