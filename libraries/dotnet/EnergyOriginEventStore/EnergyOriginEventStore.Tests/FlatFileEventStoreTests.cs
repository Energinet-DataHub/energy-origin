using EnergyOriginEventStore.Tests.Topics;
using EventStore;
using EventStore.FlatFile;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class FlatFileEventStoreTests
{
    [Fact]
    public void Test_EventStoreWorks()
    {
        try
        {
            Task.Run(async () =>
            {
                var semaphore = new SemaphoreSlim(0);
                IEventStore eventStore = new FlatFileEventStore();

                var message = new Said("Anton Actor", "I like to act!");
                await eventStore.Produce(message, new List<String> { "Gossip", "Tabloid" });

                Said? received_value = null;
                var consumer = eventStore
                    .GetBuilder("Gossip")
                    .AddHandler<Said>((value) =>
                    {
                        received_value = value;
                        semaphore.Release();
                    })
                    .Build();

                await semaphore.WaitAsync(500);

                Assert.NotNull(received_value);
                Assert.Equal(message.Actor, received_value?.Actor);
                Assert.Equal(message.Statement, received_value?.Statement);

                consumer.Dispose();
            }).Wait();
        }
        finally
        {
            Directory.Delete("store", true);
        }
    }
}