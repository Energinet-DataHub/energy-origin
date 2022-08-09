using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumer : IEventConsumer
{
    private readonly MemoryEventStore _store;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly string? _pointer;

    public MemoryEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, MemoryEventStore store, string topicPrefix, string? pointer)
    {
        _store = store;
        _unpacker = unpacker;
        _handlers = handlers;
        _pointer = pointer;

        _store.OnMessage += OnMessage; // FIXME: verify these events works

        Task.Run(() => this.Reload()).Wait();
    }

    private async Task Reload()
    {
        await _store.Reload(this);
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        var reconstructedEvent = _unpacker.UnpackEvent(e.Message);
        if (!_store.IsIncluded(_pointer, e.Pointer))
        {
            return;
        }

        var reconstructed = _unpacker.UnpackModel(reconstructedEvent);
        var type = reconstructed.GetType();
        var handler = _handlers.GetValueOrDefault(type) ?? throw new NotImplementedException($"No handler for event of type {type.ToString()}");

        handler.AsParallel().ForAll(x => x.Invoke(new Event<EventModel>(reconstructed, e.Pointer)));
    }

    public void Dispose() { }
}
