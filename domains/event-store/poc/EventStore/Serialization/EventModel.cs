using System;
using Newtonsoft.Json;

namespace EventStore.Serialization;

public abstract class EventModel {
    [JsonIgnore]
    public abstract string Type { get; }
    [JsonIgnore]
    public abstract int Version { get; }
    [JsonIgnore]
    public string Data { get { return JsonConvert.SerializeObject(this); } }
}
