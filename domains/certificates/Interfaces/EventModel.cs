using System.Reflection;
using Newtonsoft.Json;

namespace Interfaces;

public abstract record EventModel
{
    private readonly EventModelVersionAttribute attribute;

    [System.Text.Json.Serialization.JsonIgnore]
    public string Type => attribute.Type;

    [System.Text.Json.Serialization.JsonIgnore]
    public int Version => attribute.Version;

    [System.Text.Json.Serialization.JsonIgnore]
    public string Data => JsonConvert.SerializeObject(this);

    protected EventModel() => attribute = GetType().GetCustomAttribute<EventModelVersionAttribute>(false) ?? throw new NotSupportedException("All classes extending from EventModel must specify EventModelVersionAttribute");

    public virtual EventModel? NextVersion() => null;
}


[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
public class EventModelVersionAttribute : Attribute
{
    public string Type { get; }

    public int Version { get; }

    public EventModelVersionAttribute(string type, int version)
    {
        Type = type;
        Version = version;
    }
}

//[EventModelVersion("Said", 1)]
//public record Said(string Actor, string Statement) : EventModel;

