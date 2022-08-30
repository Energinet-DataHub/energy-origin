using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryEventConsumer : IEventConsumer
{
    private readonly IUnpacker unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers;
    private readonly Action<string, Exception>? exceptionHandler;
    private readonly MemoryPointer? pointer;
    private readonly string topicPrefix;

    public MemoryEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, MemoryEventStore store, string topicPrefix, MemoryPointer? pointer)
    {
        this.unpacker = unpacker;
        this.handlers = handlers;
        this.topicPrefix = topicPrefix;
        this.pointer = pointer;
        this.exceptionHandler = exceptionHandler;

        Task.Run(async () => await store.Attach(this).ConfigureAwait(false));
    }

    internal void OnMessage(object? sender, MessageEventArgs e)
    {
        if (!e.Topic.StartsWith(topicPrefix)) return;

        var reconstructedEvent = unpacker.UnpackEvent(e.Message);
        if (pointer != null && !e.Pointer.IsAfter(pointer)) return;

        var reconstructed = unpacker.UnpackModel(reconstructedEvent);
        var type = reconstructed.GetType();
        var typeString = type.ToString();

        var handlers = this.handlers.GetValueOrDefault(type);
        if (handlers == null)
        {
            exceptionHandler?.Invoke(typeString, new NotImplementedException($"No handler for event of type {type}"));
        }

        (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
        {
            try
            {
                x.Invoke(new Event<EventModel>(reconstructed, e.Pointer.Serialized));
            }
            catch (Exception exception)
            {
                exceptionHandler?.Invoke(typeString, exception);
            }
        });
    }

    public void Dispose() { }
}
