using System;
using EnergyOriginDateTimeExtension;

namespace EventStore.Serialization;

public class Event {
    public string Id { get; }
    
    public long Issued { get; }
    public int IssuedFraction { get; }
    
    public string ModelType { get; }
    public int ModelVersion { get; }
    public string Data { get; }

    public Event(string id, long issued, int issuedFraction, string modelType, int modelVersion, string data) {
        Id = id;
        Issued = issued;
        IssuedFraction = issuedFraction;
        ModelType = modelType;
        ModelVersion = modelVersion;
        Data = data;
    }

    public static Event From(EventModel model) {
        var now = DateTime.Now;

        return new Event(
            Guid.NewGuid().ToString(),
            now.ToUnixTime(),
            now.Millisecond,
            model.Type,
            model.Version,
            model.Data
        );
    }
}