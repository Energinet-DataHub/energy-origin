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

    #region IEventStore

    public async Task Produce(EventModel model, params string[] topics)
    {
        var message = InternalEvent.From(model);

        foreach (var topic in topics)
        {
            var item = new MessageEventArgs(JsonConvert.SerializeObject(message), topic, Pointer(message));

            _actions.Enqueue(() =>
            {
                _messages.Add(item);

                // FIXME: must handle topic filtering
                OnMessage?.Invoke(this, item);
            });
        }

        await Drain().ConfigureAwait(false);
    }

    public IEventConsumerBuilder GetBuilder(string topicPrefix)
    {
        return new MemoryEventConsumerBuilder(this, topicPrefix);
    }

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

    private string Pointer(InternalEvent model)
    {
        return $"{model.Issued}-{model.IssuedFraction}";
    }
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

    internal bool IsIncluded(string? continuationPointer, string messagePointer)
    {
        if (continuationPointer == null) return true;

        var continuationValues = PointerValues(continuationPointer);
        var messageValues = PointerValues(messagePointer);

        return messageValues.Item1 > continuationValues.Item1 || (messageValues.Item1 == continuationValues.Item1 && messageValues.Item2 > continuationValues.Item2);
    }

    internal async Task Reload(MemoryEventConsumer consumer)
    {
        _actions.Enqueue(async () =>
        {
            foreach (var message in _messages)
            {
                _actions.Enqueue(() =>
                {
                    // FIXME: must only be for this consumer!
                    // FIXME: must handle topic filtering
                    OnMessage?.Invoke(this, message);
                });
            }

            await Drain().ConfigureAwait(false);
        });

        await Drain().ConfigureAwait(false);
    }

    #endregion

    #region Events

    public event MessageEventHandler? OnMessage;

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    #endregion
}
