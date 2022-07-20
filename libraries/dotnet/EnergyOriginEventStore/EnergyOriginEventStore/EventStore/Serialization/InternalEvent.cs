using EnergyOriginDateTimeExtension;

namespace EnergyOriginEventStore.EventStore.Serialization;

public class InternalEvent
{
    public string Id { get; }

    public long Issued { get; }
    public int IssuedFraction { get; }

    public string ModelType { get; }
    public int ModelVersion { get; }
    public string Data { get; }

    public InternalEvent(string id, long issued, int issuedFraction, string modelType, int modelVersion, string data)
    {
        Id = id;
        Issued = issued;
        IssuedFraction = issuedFraction;
        ModelType = modelType;
        ModelVersion = modelVersion;
        Data = data;
    }

    public static InternalEvent From(EventModel model)
    {
        var now = DateTime.Now;

        return new InternalEvent(
            Guid.NewGuid().ToString(),
            now.ToUnixTime(),
            now.Millisecond,
            model.Type,
            model.Version,
            model.Data
        );
    }
}