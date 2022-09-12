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
    private readonly PeriodicTimer timer = new(TimeSpan.FromMilliseconds(100));
    private long pointer;

    public DatabaseEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, DatabaseEventContext context, string topicPrefix, string pointer)
    {
        this.context = context;
        this.unpacker = unpacker;
        this.handlers = handlers;
        this.topicPrefix = topicPrefix;
        this.pointer = long.Parse(pointer);
        this.exceptionHandler = exceptionHandler;

        Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync()) await Work();
        });
    }

    public void Dispose()
    {
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task Work()
    {
        do
        {
            Message? message;
            try
            {
                message = await context.NextAfter(this.pointer, topicPrefix);
            }
            catch (Exception exception)
            {
                exceptionHandler?.Invoke("InternalErrorWhileFetchingNextMessage", exception);
                break;
            }

            var id = message?.Id;
            if (message == null || id == null)
            {
                break;
            }

            var reconstructedEvent = unpacker.UnpackEvent(message.Payload);
            var reconstructed = unpacker.UnpackModel(reconstructedEvent);

            var pointer = $"{id}";
            var type = reconstructed.GetType();
            var typeString = type.ToString();

            var model = new Event<EventModel>(reconstructed, pointer);

            var handlers = this.handlers.GetValueOrDefault(type);
            if (handlers == null)
            {
                exceptionHandler?.Invoke(typeString, new NotImplementedException($"No handler for event of type {type}"));
            }

            this.pointer = (long)id;
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
        } while (true);
    }
}
