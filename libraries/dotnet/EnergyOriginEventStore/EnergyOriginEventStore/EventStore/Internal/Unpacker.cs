using EnergyOriginEventStore.EventStore.Serialization;
using Newtonsoft.Json;
using System.Reflection;

namespace EnergyOriginEventStore.EventStore.Internal;

internal class Unpacker : IUnpacker
{
    private readonly Dictionary<Tuple<string, int>, Type> _typeDictionary;

    public Unpacker()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsDefined(typeof(EventModelVersionAttribute)));
        _typeDictionary = new Dictionary<Tuple<string, int>, Type>();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<EventModelVersionAttribute>() ?? throw new NotSupportedException("Attribute not found on type that should have it.");
            _typeDictionary.Add(Tuple.Create(attribute.Type, attribute.Version), type);
        }
    }

    public InternalEvent UnpackEvent(string payload)
    {
        return JsonConvert.DeserializeObject<InternalEvent>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");
    }

    public T UnpackModel<T>(InternalEvent payload) where T : EventModel
    {
        return (T)UnpackModel(payload) ?? throw new InvalidCastException("Event not of correct type");
    }

    public EventModel UnpackModel(InternalEvent payload)
    {
        var type = _typeDictionary.GetValueOrDefault(Tuple.Create(payload.ModelType, payload.ModelVersion)) ??
                   throw new NotSupportedException($"Could not find type to unpack event type:\"{payload.ModelType}\" version:\"{payload.ModelVersion}\"");

        var current = JsonConvert.DeserializeObject(payload.Data, type) as EventModel ?? throw new Exception("Type not an EventModel!");

        while (true)
        {
            var next = current.NextVersion();

            if (next is null)
            {
                return current;
            }

            current = next;
        }
    }
}
