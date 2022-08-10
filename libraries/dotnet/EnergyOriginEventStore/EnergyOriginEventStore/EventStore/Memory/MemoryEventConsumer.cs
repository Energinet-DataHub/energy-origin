using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumer : IEventConsumer
{
    private readonly MemoryEventStore _store;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly string? _pointer;
    private readonly string _topicPrefix;

    public MemoryEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, MemoryEventStore store, string topicPrefix, string? pointer)
    {
        _store = store;
        _unpacker = unpacker;
        _handlers = handlers;
        _topicPrefix = topicPrefix;
        _pointer = pointer;

        Task.Run(async () => await _store.Attach(this).ConfigureAwait(false));
    }

    internal void OnMessage(object sender, MessageEventArgs e)
    {
        if (!e.Topic.StartsWith(_topicPrefix)) return;

        var reconstructedEvent = _unpacker.UnpackEvent(e.Message);
        if (!_store.ShouldInclude(_pointer, e.Pointer)) return;

        var reconstructed = _unpacker.UnpackModel(reconstructedEvent);
        var type = reconstructed.GetType();
        var handler = _handlers.GetValueOrDefault(type) ?? throw new NotImplementedException($"No handler for event of type {type.ToString()}");

        handler.AsParallel().ForAll(x => x.Invoke(new Event<EventModel>(reconstructed, e.Pointer)));
    }

    public void Dispose() { }
}
