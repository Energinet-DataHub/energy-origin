using EnergyOriginEventStore.Tests.Topics;
using EventStore;
using EventStore.FlatFile;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class FlatFileEventStoreTests
{
    [Fact]
    public async Task EventStore_Works_success()
    {
        try
        {
            var semaphore = new SemaphoreSlim(0);
            IEventStore eventStore = new FlatFileEventStore();

            var message = new Said("Anton Actor", "I like to act!");
            await eventStore.Produce(message, "Gossip", "Tabloid");

            Said? receivedValue = null;
            var consumer = eventStore
                .GetBuilder("Gossip")
                .AddHandler<Said>((value) =>
                {
                    receivedValue = value.EventModel;
                    semaphore.Release();
                })
                .Build();

            await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500));

            Assert.NotNull(receivedValue);
            Assert.Equal(message.Actor, receivedValue?.Actor);
            Assert.Equal(message.Statement, receivedValue?.Statement);

            consumer.Dispose();
        }
        finally
        {
            Directory.Delete("store", true);
        }
    }

    [Fact]
    public async Task EventStore_ResumeFromPointer_success()
    {
        try
        {
            string? pointer = null;

            const string message1 = "I like to act!";
            const string message2 = "I want another helicopter";
            const string message3 = "I feel poor, because i only have one yacht!";

            using (var eventStore = new FlatFileEventStore())
            {
                var semaphore = new SemaphoreSlim(0);

                var message = new Said("Anton Actor", message1);
                await eventStore.Produce(message, "Gossip");

                message = new Said("Anton Actor", message2);
                await eventStore.Produce(message, "Gossip");

                message = new Said("Anton Actor", message3);
                await eventStore.Produce(message, "Gossip");

                var consumer = eventStore
                    .GetBuilder("Gossip")
                    .AddHandler<Said>((value) =>
                    {
                        if (value.EventModel.Statement == message2)
                        {
                            pointer = value.Pointer;
                            semaphore.Release();
                        }
                    })
                    .Build();

                await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500));

                Assert.NotNull(pointer);
            }

            using (var eventStore = new FlatFileEventStore())
            {
                var semaphore = new SemaphoreSlim(0);
                var received = new List<Said>();

                var consumer = eventStore
                    .GetBuilder("Gossip")
                    .ContinueFrom(pointer!)
                    .AddHandler<Said>((value) =>
                    {
                        received.Add(value.EventModel);
                    })
                    .Build();

                await Task.Delay(TimeSpan.FromMilliseconds(500));

                Assert.Single(received);
                Assert.Equal(message3, received.Single().Statement);
            }
        }
        finally
        {
            Directory.Delete("store", true);
        }
    }
}