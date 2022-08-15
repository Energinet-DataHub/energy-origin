using System.Collections.Concurrent;
using EnergyOriginEventStore.EventStore.Serialization;
using Newtonsoft.Json;

namespace EnergyOriginEventStore.EventStore.Memory;

public class MemoryEventStore : IEventStore
{
    private List<MessageEventArgs> _messages = new();
    private ConcurrentQueue<Action> _actions = new();
    [ThreadStatic] private bool isDraining = false;

    public MemoryEventStore() { }

    internal async Task Attach(MemoryEventConsumer consumer)
    {
        // NOTE: The following will enqueue an action that in turn will enqueue an action for each known message and then an action to begin listening for new messages.
        // By letting a queued action perform the message deliveries and actual attaching, we avoid dealing with new messages during attachment.

        _actions.Enqueue(async () =>
        {
            foreach (var message in _messages)
            {
                _actions.Enqueue(() =>
                {
                    consumer.OnMessage(this, message);
                });
            }

            _actions.Enqueue(() =>
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
            var item = new MessageEventArgs(JsonConvert.SerializeObject(message), topic, ToPointer(message));

            _actions.Enqueue(() =>
            {
                _messages.Add(item);
                OnMessage?.Invoke(this, item);
            });
        }

        await Drain().ConfigureAwait(false);
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix) => new MemoryEventConsumerBuilder(this, topicPrefix);

    public event Action? DisposeEvent;

    public void Dispose()
    {
        _actions.Clear();
        DisposeEvent?.Invoke();
    }

    private async Task Drain()
    {
        if (isDraining) return;

        isDraining = true;

        Action? item;
        while (_actions.TryDequeue(out item))
        {
            item?.Invoke();
        }

        isDraining = false;

        if (!_actions.IsEmpty)
        {
            await Drain();
        }
    }

    #endregion

    #region Pointers

    private string ToPointer(InternalEvent model) => $"{model.Issued}-{model.IssuedFraction}";

    private Tuple<long, long> PointerValues(string pointer)
    {
        long issued;
        long fraction;
        try
        {
            var parts = pointer.Split('-');
            issued = long.Parse(parts[0]);
            fraction = long.Parse(parts[1]);
        }
        catch (Exception)
        {
            throw new InvalidDataException($"Pointer '{pointer}' not a valid format");
        }
        return new Tuple<long, long>(issued, fraction);
    }

    internal bool ShouldInclude(string? continuationPointer, string messagePointer)
    {
        if (continuationPointer == null) return true;

        var continuationValues = PointerValues(continuationPointer);
        var messageValues = PointerValues(messagePointer);

        return messageValues.Item1 > continuationValues.Item1 || (messageValues.Item1 == continuationValues.Item1 && messageValues.Item2 > continuationValues.Item2);
    }

    #endregion

    #region Events

    public event MessageEventHandler? OnMessage;

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    #endregion
}
