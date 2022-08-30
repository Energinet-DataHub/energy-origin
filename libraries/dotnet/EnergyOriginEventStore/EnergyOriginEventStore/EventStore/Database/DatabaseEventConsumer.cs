using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Database;

internal class DatabaseEventConsumer : IEventConsumer
{
    private readonly DatabaseEventContext context;
    private readonly IUnpacker unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers;
    private readonly Action<string, Exception>? exceptionHandler;
    private readonly string topicPrefix;
    private readonly PeriodicTimer timer;
    private string? pointer;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public DatabaseEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, DatabaseEventContext context, string topicPrefix, string? pointer)
    {
        this.context = context;
        this.unpacker = unpacker;
        this.handlers = handlers;
        this.topicPrefix = topicPrefix;
        this.pointer = pointer;
        this.exceptionHandler = exceptionHandler;

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
        GC.SuppressFinalize(this);
    }

    private async Task Work() // FIXME: to be called on produce, if needed
    {
        await semaphore.WaitAsync();

        IQueryable<Message> query = context.Messages.OrderBy(it => it.Id);
        if (pointer != null)
        {
            query = query.Where(it => it.Id > long.Parse(pointer ?? ""));
        }
        query = query.Where(it => it.Topic.StartsWith(topicPrefix));

        foreach (var message in query.ToList())
        {
            if (message == null) continue;

            var reconstructedEvent = unpacker.UnpackEvent(message.Payload);
            var reconstructed = unpacker.UnpackModel(reconstructedEvent);

            var pointer = $"{message.Id}";
            var type = reconstructed.GetType();
            var typeString = type.ToString();

            var model = new Event<EventModel>(reconstructed, pointer);

            var handlers = this.handlers.GetValueOrDefault(type);
            if (handlers == null)
            {
                exceptionHandler?.Invoke(typeString, new NotImplementedException($"No handler for event of type {type}"));
            }

            this.pointer = pointer;
            await Task.WhenAll((handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).Select(it => Task.Run(() =>
            {
                try
                {
                    it.Invoke(model);
                }
                catch (Exception exception)
                {
                    exceptionHandler?.Invoke(typeString, exception);
                }
            })));
        }

        semaphore.Release();
    }
}
