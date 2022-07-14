using System;
using System.Reflection;
using EnergyOriginDateTimeExtension;
using EventStore.Serialization;
using Newtonsoft.Json;

namespace EventStore;

public class Unpacker : IUnpacker
{
    private Dictionary<Tuple<string, int>, Type> typeDictionary;

    public Unpacker()
    {

        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsDefined(typeof(EventModelVersionAttribute)));
        this.typeDictionary = new Dictionary<Tuple<string, int>, Type>();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<EventModelVersionAttribute>() ?? throw new NotSupportedException("Attribute not found on type that should have it.");
            this.typeDictionary.Add(Tuple.Create(attribute.Type, attribute.Version), type);
        }
    }

    public Event UnpackEvent(string payload)
    {
        return JsonConvert.DeserializeObject<Event>(payload) ?? throw new Exception($"Payload could not be decoded: {payload}");
    }

    public T UnpackModel<T>(Event payload) where T : EventModel
    {
        return (T)UnpackModel(payload) ?? throw new InvalidCastException("Event not of correct type");
    }

    public EventModel UnpackModel(Event payload)
    {
        Type type;
        if (this.typeDictionary.TryGetValue(Tuple.Create(payload.ModelType, payload.ModelVersion), out type))
        {
            return JsonConvert.DeserializeObject(payload.Data, type) as EventModel ?? throw new Exception("Type not an EventModel!");
        }
        else
        {
            throw new NotSupportedException($"Could not find type to unpack event type:\"{payload.ModelType}\" version:\"{payload.ModelVersion}\"");
        }
    }
}