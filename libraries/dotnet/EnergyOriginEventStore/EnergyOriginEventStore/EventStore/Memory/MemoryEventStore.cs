using System.Collections.Concurrent;
using EnergyOriginEventStore.EventStore.Serialization;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Memory;

public class MemoryEventStore : IEventStore
{
    private readonly List<MessageEventArgs> messages = new();
    private readonly ConcurrentQueue<Action> actions = new();
    private readonly PeriodicTimer timer = new(TimeSpan.FromMilliseconds(100));
    private static readonly SemaphoreSlim drainSemaphore = new(1, 1);

    public MemoryEventStore() => Task.Run(async () =>
    {
        while (await timer.WaitForNextTickAsync()) await Drain();
    });

    internal event EventHandler<MessageEventArgs>? OnMessage;

    internal void Attach(MemoryEventConsumer consumer) => actions.Enqueue(() =>
        {
            foreach (var message in messages)
            {
                consumer.OnMessage(this, message);
            }

            OnMessage += consumer.OnMessage;
        }
    );

    #region IEventStore

    Task IEventStore.Produce(EventModel model, params string[] topics)
    {
        var message = InternalEvent.From(model);

        foreach (var topic in topics)
        {
            var item = new MessageEventArgs(JsonConvert.SerializeObject(message), topic, new MemoryPointer(message));

            actions.Enqueue(() =>
            {
                messages.Add(item);
                OnMessage?.Invoke(this, item);
            });
        }

        return Task.CompletedTask;
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new MemoryEventConsumerBuilder(this, topicPrefix);

    public void Dispose()
    {
        actions.Clear();
        GC.SuppressFinalize(this);
    }

    private async Task Drain()
    {
        if (drainSemaphore.CurrentCount == 0) return;

        await drainSemaphore.WaitAsync();

        while (actions.TryDequeue(out var item))
        {
            item?.Invoke();
        }

        drainSemaphore.Release();

        if (!actions.IsEmpty)
        {
            await Drain();
        }
    }

    #endregion
}
