namespace EnergyOriginEventStore.EventStore.Serialization;

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
