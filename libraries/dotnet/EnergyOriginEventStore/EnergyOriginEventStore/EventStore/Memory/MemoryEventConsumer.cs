using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumer : IEventConsumer
{
    private readonly MemoryEventStore _store;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly Action<string, Exception> _exceptionHandler;
    private readonly MemoryPointer? _pointer;
    private readonly string _topicPrefix;

    public MemoryEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, MemoryEventStore store, string topicPrefix, MemoryPointer? pointer)
    {
        _store = store;
        _unpacker = unpacker;
        _handlers = handlers;
        _topicPrefix = topicPrefix;
        _pointer = pointer;
        _exceptionHandler = exceptionHandler ?? ((type, exception) => Console.WriteLine($"Type: {type} - Message: {exception.Message}"));

        Task.Run(async () => await _store.Attach(this).ConfigureAwait(false));
    }

    internal void OnMessage(object? sender, MessageEventArgs e)
    {
        if (!e.Topic.StartsWith(_topicPrefix)) return;

        var reconstructedEvent = _unpacker.UnpackEvent(e.Message);
        if (_pointer != null && !e.Pointer.IsAfter(_pointer)) return;

        var reconstructed = _unpacker.UnpackModel(reconstructedEvent);
        var type = reconstructed.GetType();
        var typeString = type.ToString();

        var handlers = _handlers.GetValueOrDefault(type);
        if (handlers == null)
        {
            _exceptionHandler.Invoke(typeString, new NotImplementedException($"No handler for event of type {type.ToString()}"));
        }

        (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
        {
            try
            {
                x.Invoke(new Event<EventModel>(reconstructed, e.Pointer.Serialized));
            }
            catch (Exception exception)
            {
                _exceptionHandler.Invoke(typeString, exception);
            }
        });
    }

    public void Dispose() { }
}
