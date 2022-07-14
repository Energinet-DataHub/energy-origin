using System;
using Newtonsoft.Json;

namespace EventStore.Serialization;


[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
public class EventModelVersionAttribute : Attribute
{
    public string Type { get; }

    public int Version { get; }

    // Constructor
    public EventModelVersionAttribute(string type, int version)
    {
        this.Type = type;
        this.Version = version;
    }
}
