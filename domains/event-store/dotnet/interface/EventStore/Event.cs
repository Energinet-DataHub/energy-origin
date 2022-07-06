using System;

namespace EventStore;

public class Event {
    public readonly string Id;
    
    public readonly long Issued;
    public readonly int IssuedFraction;
    
    public readonly string ModelType;
    public readonly int ModelVersion;
    public readonly string Data;

    public Event(string id, long issued, int issuedFraction, string modelType, int modelVersion, string data) {
        Id = id;
        Issued = issued;
        IssuedFraction = issuedFraction;
        ModelType = modelType;
        ModelVersion = modelVersion;
        Data = data;
    }

    public Event FromModel(EventModel model) {
        var now = ((DateTimeOffset)DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));

        return new Event(
            Guid.NewGuid().ToString(),
            now.ToUnixTimeSeconds(),
            now.Millisecond,
            model.Type,
            model.Version,
            model.Data
        );
    }
}