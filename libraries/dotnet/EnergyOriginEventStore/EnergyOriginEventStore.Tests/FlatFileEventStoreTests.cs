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

            Said? received_value = null;
            var consumer = eventStore
                .GetBuilder("Gossip")
                .AddHandler<Said>((value) =>
                {
                    received_value = value.EventModel;
                    semaphore.Release();
                })
                .Build();

            await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500));

            Assert.NotNull(received_value);
            Assert.Equal(message.Actor, received_value?.Actor);
            Assert.Equal(message.Statement, received_value?.Statement);

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

            string message_1 = "I like to act!";
            string message_2 = "I want another helicopter";
            string message_3 = "I feel poor, because i only have one yacht!";

            using (var eventStore = new FlatFileEventStore())
            {
                var semaphore = new SemaphoreSlim(0);

                var message = new Said("Anton Actor", message_1);
                await eventStore.Produce(message, "Gossip");

                message = new Said("Anton Actor", message_2);
                await eventStore.Produce(message, "Gossip");

                message = new Said("Anton Actor", message_3);
                await eventStore.Produce(message, "Gossip");

                var consumer = eventStore
                    .GetBuilder("Gossip")
                    .AddHandler<Said>((value) =>
                    {
                        if (value.EventModel.Statement == message_2)
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
                List<Said> received = new List<Said>();

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
                Assert.Equal(message_3, received.Single().Statement);
            }
        }
        finally
        {
            Directory.Delete("store", true);
        }
    }
}