using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Database;

internal class DatabaseEventConsumer : IEventConsumer
{
    private readonly DatabaseEventContext _context;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly Action<string, Exception>? _exceptionHandler;
    private readonly string? _pointer;
    private readonly string _topicPrefix;

    public DatabaseEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, DatabaseEventContext context, string topicPrefix, string? pointer)
    {
        _context = context;
        _unpacker = unpacker;
        _handlers = handlers;
        _topicPrefix = topicPrefix;
        _pointer = pointer;
        _exceptionHandler = exceptionHandler;

        // Task.Run(async () => await _store.Attach(this).ConfigureAwait(false));
    }

    // internal void OnMessage(object? sender, MessageEventArgs e)
    // {
    //     if (!e.Topic.StartsWith(_topicPrefix)) return;

    //     var reconstructedEvent = _unpacker.UnpackEvent(e.Message);
    //     if (_pointer != null && !e.Pointer.IsAfter(_pointer)) return;

    //     var reconstructed = _unpacker.UnpackModel(reconstructedEvent);
    //     var type = reconstructed.GetType();
    //     var typeString = type.ToString();

    //     var handlers = _handlers.GetValueOrDefault(type);
    //     if (handlers == null)
    //     {
    //         _exceptionHandler?.Invoke(typeString, new NotImplementedException($"No handler for event of type {type.ToString()}"));
    //     }

    //     (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
    //     {
    //         try
    //         {
    //             x.Invoke(new Event<EventModel>(reconstructed, e.Pointer.Serialized));
    //         }
    //         catch (Exception exception)
    //         {
    //             _exceptionHandler?.Invoke(typeString, exception);
    //         }
    //     });
    // }

    public void Dispose() { }
}
