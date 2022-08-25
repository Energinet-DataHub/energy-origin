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
    private readonly PeriodicTimer timer;

    public DatabaseEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, DatabaseEventContext context, string topicPrefix, string? pointer)
    {
        _context = context;
        _unpacker = unpacker;
        _handlers = handlers;
        _topicPrefix = topicPrefix;
        _pointer = pointer;
        _exceptionHandler = exceptionHandler;

        timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
    }

    internal async Task Begin()
    {
        while (await timer.WaitForNextTickAsync())
        {
            var query = _context.Messages.OrderBy(it => it.Id);
            if (_pointer != null)
            {
                var pointer = Int64.Parse(_pointer ?? "");
                query.Where(it => it.Id > pointer);
            }
            query.Where(it => it.Topic.StartsWith(_topicPrefix));

            foreach (var message in query)
            {
                if (message == null) continue;

                var reconstructedEvent = _unpacker.UnpackEvent(message.Payload);
                var reconstructed = _unpacker.UnpackModel(reconstructedEvent);

                var type = reconstructed.GetType();
                var typeString = type.ToString();

                var handlers = _handlers.GetValueOrDefault(type);
                if (handlers == null)
                {
                    _exceptionHandler?.Invoke(typeString, new NotImplementedException($"No handler for event of type {type.ToString()}"));
                }

                (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
                {
                    try
                    {
                        x.Invoke(new Event<EventModel>(reconstructed, $"{message.Id}"));
                    }
                    catch (Exception exception)
                    {
                        _exceptionHandler?.Invoke(typeString, exception);
                    }
                });
            }
        }
    }

    public void Dispose() { }
}
