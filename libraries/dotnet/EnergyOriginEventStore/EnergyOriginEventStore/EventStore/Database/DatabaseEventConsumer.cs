using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Database;

internal class DatabaseEventConsumer : IEventConsumer
{
    private readonly DatabaseEventContext _context;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly Action<string, Exception>? _exceptionHandler;
    private readonly string _topicPrefix;
    private readonly PeriodicTimer timer;
    private string? _pointer;
    private SemaphoreSlim semaphore = new(0, 1);

    public DatabaseEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, DatabaseEventContext context, string topicPrefix, string? pointer)
    {
        _context = context;
        _unpacker = unpacker;
        _handlers = handlers;
        _topicPrefix = topicPrefix;
        _pointer = pointer;
        _exceptionHandler = exceptionHandler;

        timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

        Task.Run(async () =>
        {
            do
            {
                await Work();
            } while (await timer.WaitForNextTickAsync());
        });
    }

    public void Dispose()
    {
        timer.Dispose();
    }

    async Task Work() // FIXME: to be called on produce
    {
        await semaphore.WaitAsync();

        IQueryable<Message> query = _context.Messages.OrderBy(it => it.Id);
        if (_pointer != null)
        {
            var pointer = Int64.Parse(_pointer ?? "");
            query = query.Where(it => it.Id > pointer);
        }
        query = query.Where(it => it.Topic.StartsWith(_topicPrefix));

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

            var pointer = $"{message.Id}";
            _pointer = pointer;

            var ghasdj = new Event<EventModel>(reconstructed, pointer);
            var items = (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>());

            await Task.WhenAll(items.Select(it => Task.Run(() =>
            {
                try
                {
                    it.Invoke(ghasdj);
                }
                catch (Exception exception)
                {
                    _exceptionHandler?.Invoke(typeString, exception);
                }
            })));

            semaphore.Release();
        }
    }
}
