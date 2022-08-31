using EnergyOriginEventStore.EventStore;
using EnergyOriginEventStore.EventStore.Database;
using EnergyOriginEventStore.EventStore.FlatFile;
using EnergyOriginEventStore.EventStore.Memory;
using EnergyOriginEventStore.Tests.Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

[Collection("Database collection")]
public class EventStoreTests : IClassFixture<EventStoreTests.DatabaseFixture>, IDisposable
{
    #region Setup

    private readonly DatabaseFixture fixture;

    public EventStoreTests(DatabaseFixture fixture) => this.fixture = fixture;

    private static IEnumerable<object[]> Data() => new List<object[]>
        {
            new object[] { new DatabaseBuilder(), true },
            new object[] { new MemoryBuilder(), false },
            new object[] { new FlatFileBuilder(), true }
        };

    #endregion

    #region Tests

#pragma warning disable IDE0060, xUnit1026

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_CanReceiveAMessage_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
        var semaphore = new SemaphoreSlim(0);
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

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.NotNull(receivedValue);
        Assert.Equal(message.Actor, receivedValue?.Actor);
        Assert.Equal(message.Statement, receivedValue?.Statement);

        consumer.Dispose();
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_CanResumeFromGivenPointer_Success(Builder builder, bool canPersist)
    {
        if (!canPersist) return;

        string? pointer = null;

        const string message1 = "I like to act!";
        const string message2 = "I want another helicopter";
        const string message3 = "I feel poor, because i only have one yacht!";

        using (var eventStore = await builder.build(fixture))
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
                .AddHandler<Said>(value =>
                {
                    if (value.EventModel.Statement == message2)
                    {
                        pointer = value.Pointer;
                        semaphore.Release();
                    }
                })
                .Build();

            await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

            Assert.NotNull(pointer);
        }

        using (var eventStore = await builder.build(fixture, false))
        {
            var semaphore = new SemaphoreSlim(0);

            var received = new List<Said>();

            var consumer = eventStore
                .GetBuilder("Gossip")
                .ContinueFrom(pointer!)
                .AddHandler<Said>(value =>
                {
                    semaphore.Release();
                    received.Add(value.EventModel);
                })
                .Build();

            await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

            Assert.Single(received);
            Assert.Equal(message3, received.Single().Statement);
        }
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_CanResumeFromPointerUsingSingleStore_Success(Builder builder, bool canPersist)
    {
        string? pointer = null;

        const string message1 = "I like to act!";
        const string message2 = "I want another helicopter";
        const string message3 = "I feel poor, because i only have one yacht!";

        var eventStore = await builder.build(fixture);

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

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.NotNull(pointer);

        var received = new List<Said>();

        eventStore
            .GetBuilder("Gossip")
            .ContinueFrom(pointer!)
            .AddHandler<Said>(value =>
            {
                received.Add(value.EventModel);
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.Single(received);
        Assert.Equal(message3, received.Single().Statement);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_EnsureExceptionHandlerIsCalled_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
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

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.True(hadException);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_VerifyExceptionsFromHandlersAreSwallowed_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
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

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.True(hasThrownException);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_EnsureCallExceptionHandlerWhenNoHandlerIsFound_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
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

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.True(hadException);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_CanFilterMessagesBasedOnTopics_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
        var semaphore = new SemaphoreSlim(0);

        var message = new Said("Samuel Salesman", "We have been trying to reach you about your cars extended warranty!");
        await eventStore.Produce(message, "Spam", "Advertisement", "Robocall");

        var received = new List<Said>();
        eventStore
            .GetBuilder("Advertisement")
            .AddHandler<Said>(value =>
            {
                received.Add(value.EventModel);
                semaphore.Release();
            })
            .Build();

        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.Single(received);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_CanSupportMultipleListeners_Success(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
        var semaphore1 = new SemaphoreSlim(0);
        var semaphore2 = new SemaphoreSlim(0);
        var semaphore3 = new SemaphoreSlim(0);

        var received1 = new List<Said>();
        var received2 = new List<Said>();
        var received3 = new List<Said>();

        var message = new Said("Tony Topical", "Everybody wants to listen to me!");

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value =>
            {
                received1.Add(value.EventModel);
                semaphore1.Release();
            })
            .Build();

        await eventStore.Produce(message, "Topical");

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value =>
            {
                received2.Add(value.EventModel);
                semaphore2.Release();
            })
            .Build();

        eventStore
            .GetBuilder("Topical")
            .AddHandler<Said>(value =>
            {
                received3.Add(value.EventModel);
                semaphore3.Release();
            })
            .Build();

        await semaphore1.WaitAsync(TimeSpan.FromMilliseconds(1000));
        await semaphore2.WaitAsync(TimeSpan.FromMilliseconds(1000));
        await semaphore3.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.Single(received1);
        Assert.Single(received2);
        Assert.Single(received3);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task EventStore_EnsureEventFlow_Works(Builder builder, bool canPersist)
    {
        var eventStore = await builder.build(fixture);
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

        var message = new Said("Internet Explorer", "Ringo Starr replaces Pete Best as Beatles' drummer.");
        await eventStore.Produce(message, "OldNews");
        await semaphore.WaitAsync(TimeSpan.FromMilliseconds(1000));

        Assert.True(ensureEventFlowIsExercised);
    }

#pragma warning restore IDE0060, xUnit1026

    #endregion

    #region Fixtures

    public class DatabaseFixture : IAsyncLifetime, IDisposable
    {
        public readonly DatabaseEventContext Context = new SqliteDatabaseEventContext();

        public SqliteDatabaseEventContext OriginalContext => (SqliteDatabaseEventContext)Context;

        public async Task InitializeAsync() => await OriginalContext.Setup();

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

        public void Dispose() => GC.SuppressFinalize(this);
    }

    #endregion

    #region Builders

    public interface Builder
    {
        Task<IEventStore> build(DatabaseFixture fixture, bool clean = true);
    }

    private class MemoryBuilder : Builder
    {
        public Task<IEventStore> build(DatabaseFixture fixture, bool clean = true) => Task.FromResult(new MemoryEventStore() as IEventStore);
    }

    private class FlatFileBuilder : Builder
    {
        public Task<IEventStore> build(DatabaseFixture fixture, bool clean = true)
        {
            if (clean && Directory.Exists("store"))
            {
                Directory.Delete("store", true);
            }
            return Task.FromResult(new FlatFileEventStore() as IEventStore);
        }
    }

    private class DatabaseBuilder : Builder
    {
        public async Task<IEventStore> build(DatabaseFixture fixture, bool clean = true)
        {
            if (clean)
            {
                await fixture.OriginalContext.Clean();
            }
            return new DatabaseEventStore(fixture.Context);
        }
    }

    #endregion

    #region Disposable

    public void Dispose()
    {
        if (Directory.Exists("store"))
        {
            Directory.Delete("store", true);
        }
        GC.SuppressFinalize(this);
    }

    #endregion
}
