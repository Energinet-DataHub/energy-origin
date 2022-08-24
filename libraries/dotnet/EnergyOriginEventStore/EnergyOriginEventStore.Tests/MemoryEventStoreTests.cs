using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Memory;
using EnergyOriginEventStore.Tests.Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class MemoryEventStoreTests
{
    [Fact]
    public async Task EventStore_Works_success()
    {
        var semaphore = new SemaphoreSlim(0);
        IEventStore eventStore = new MemoryEventStore();

        var message = new Said("Anton Actor", "I like to act!");
        await eventStore.Produce(message, "Gossip", "Tabloid");

        Said? receivedValue = null;
        eventStore
            .GetBuilder("Gossip")
            .AddHandler<Said>(value =>
            {
                receivedValue = value.EventModel;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.NotNull(receivedValue);
        Assert.Equal(message.Actor, receivedValue?.Actor);
        Assert.Equal(message.Statement, receivedValue?.Statement);
    }

    [Fact]
    public async Task EventStore_ResumeFromPointer_success()
    {
        string? pointer = null;

        const string message1 = "I like to act!";
        const string message2 = "I want another helicopter";
        const string message3 = "I feel poor, because i only have one yacht!";

        var eventStore = new MemoryEventStore();

        var semaphore = new SemaphoreSlim(0);

        var message = new Said("Anton Actor", message1);
        await eventStore.Produce(message, "Gossip");

        message = new Said("Anton Actor", message2);
        await eventStore.Produce(message, "Gossip");

        message = new Said("Anton Actor", message3);
        await eventStore.Produce(message, "Gossip");

        eventStore
            .GetBuilder("Gossip")
            .AddHandler<Said>(value =>
            {
                if (value.EventModel.Statement == message2)
                {
                    pointer = value.Pointer;
                }
                if (value.EventModel.Statement == message3)
                {
                    semaphore.Release();
                }
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.NotNull(pointer);

        var received = new List<Said>();

        eventStore
            .GetBuilder("Gossip")
            .ContinueFrom(pointer!)
            .AddHandler<Said>(value => received.Add(value.EventModel))
            .Build();

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        Assert.Single(received);
        Assert.Equal(message3, received.Single().Statement);
    }

    [Fact]
    public async Task EventStore_FiltersTopics_success()
    {
        IEventStore eventStore = new MemoryEventStore();

        var message = new Said("Samuel Salesman", "We have been trying to reach you about your cars extended warranty!");
        await eventStore.Produce(message, "Spam", "Advertisement", "Robocall");

        var received = new List<Said>();
        eventStore
            .GetBuilder("Advertisement")
            .AddHandler<Said>(value => received.Add(value.EventModel))
            .Build();

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        Assert.Single(received);
    }

    [Fact]
    public async Task EventStore_MultipleListeners_success()
    {
        IEventStore eventStore = new MemoryEventStore();

        var received1 = new List<Said>();
        var received2 = new List<Said>();
        var received3 = new List<Said>();

        var message = new Said("Tony Topical", "Everybody wants to listen to me!");

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value => received1.Add(value.EventModel))
            .Build();

        await eventStore.Produce(message, "Topical");

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value => received2.Add(value.EventModel))
            .Build();

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value => received3.Add(value.EventModel))
            .Build();

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Single(received3);
    }

    [Fact]
    public async Task EventStore_ExceptionHandlerIsCalled()
    {
        IEventStore eventStore = new MemoryEventStore();
        var semaphore = new SemaphoreSlim(0);
        var hadException = false;

        var message = new Said("Exavier Exception", "I have eruptive things to say!");
        await eventStore.Produce(message, "Unstable");

        eventStore
            .GetBuilder("Unstable")
            .AddHandler<Said>(value => throw new NotImplementedException("Oh Exavier did it again..."))
            .SetExceptionHandler((type, exception) =>
            {
                hadException = true;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.True(hadException);
    }

    [Fact]
    public async Task EventStore_ExceptionsFromHandlersAreSwallowed()
    {
        IEventStore eventStore = new MemoryEventStore();
        var semaphore = new SemaphoreSlim(0);
        var hasThrownException = false;

        var message = new Said("Exavier Exception", "I have eruptive things to say!");
        await eventStore.Produce(message, "Unstable");

        eventStore
            .GetBuilder("Unstable")
            .AddHandler<Said>(_ =>
            {
                hasThrownException = true;
                semaphore.Release();
                throw new NotImplementedException("Oh Exavier did it again...");
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.True(hasThrownException);
    }

    [Fact]
    public async Task EventStore_CallExceptionHandlerWhenNoHandlerIsFound()
    {
        IEventStore eventStore = new MemoryEventStore();
        var semaphore = new SemaphoreSlim(0);
        var hadException = false;

        var message = new Said("Annie Anonymous", "No one listens to me!");
        await eventStore.Produce(message, "Void");

        eventStore
            .GetBuilder("Void")
            .SetExceptionHandler((type, exception) =>
            {
                Assert.IsType<NotImplementedException>(exception);
                hadException = true;
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.True(hadException);
    }

    [Fact]
    public async Task EventStore_EventFlow_works()
    {
        IEventStore eventStore = new MemoryEventStore();
        var semaphore = new SemaphoreSlim(0);
        var ensureEventFlowIsExercised = false;

        eventStore
            .GetBuilder("OldNews")
            .AddHandler<Said>(_ =>
            {
                ensureEventFlowIsExercised = true;
                semaphore.Release();
            })
            .Build();
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var message = new Said("Internet Explorer", "Ringo Starr replaces Pete Best as Beatles' drummer.");
        await eventStore.Produce(message, "OldNews");
        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50));

        Assert.True(ensureEventFlowIsExercised);
    }
}
