using System.Collections.Concurrent;
using EnergyOriginEventStore.EventStore.Serialization;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Memory;

public class MemoryEventStore : IEventStore
{
    private readonly List<MessageEventArgs> messages = new();
    private readonly ConcurrentQueue<Action> actions = new();
    private static SemaphoreSlim drainSemaphore = new SemaphoreSlim(1, 1);

    public MemoryEventStore() { }

    internal event EventHandler<MessageEventArgs>? OnMessage;

    internal async Task Attach(MemoryEventConsumer consumer)
    {
        // NOTE: The following will enqueue an action that in turn will enqueue an action for each known message and then an action to begin listening for new messages.
        // By letting a queued action perform the message deliveries and actual attaching, we avoid dealing with new messages during attachment.

        actions.Enqueue(async () =>
        {
            foreach (var message in messages)
            {
                actions.Enqueue(() =>
                {
                    consumer.OnMessage(this, message);
                });
            }

            actions.Enqueue(() =>
            {
                OnMessage += consumer.OnMessage;
            });

            await Drain().ConfigureAwait(false);
        });

        await Drain().ConfigureAwait(false);
    }

    #region IEventStore

    public async Task Produce(EventModel model, params string[] topics)
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

        await Drain().ConfigureAwait(false);
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new MemoryEventConsumerBuilder(this, topicPrefix);

    public void Dispose()
    {
        actions.Clear();
    }

    private async Task Drain()
    {
        if (drainSemaphore.CurrentCount == 0) return;

        drainSemaphore.Wait();

        Action? item;
        while (actions.TryDequeue(out item))
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
