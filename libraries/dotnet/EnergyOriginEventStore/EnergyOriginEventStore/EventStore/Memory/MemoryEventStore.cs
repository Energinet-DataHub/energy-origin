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
                _messages.Append(item);
                OnMessage?.Invoke(this, item); // FIXME: must handle topic filtering
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
        return $"{model.Issued}-{model.IssuedFraction}"; // FIXME: better, maybe vectorclock?
    }

    internal bool IsIncluded(string? continuationPointer, string messagePointer)
    {
        if (continuationPointer == null) return true;

        return false; // FIXME: finish this
    }

    internal async Task Reload(MemoryEventConsumer consumer)
    {
        foreach (var message in _messages)
        {
            _actions.Enqueue(() =>
            {
                OnMessage?.Invoke(this, message); // FIXME: must handle topic filtering
            });
        }

        await Drain().ConfigureAwait(false);
    }

    #endregion

    #region Events

    public event MessageEventHandler? OnMessage;

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);

    #endregion
}
