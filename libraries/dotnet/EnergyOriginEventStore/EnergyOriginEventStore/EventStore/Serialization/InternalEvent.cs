using EnergyOriginDateTimeExtension;

namespace EnergyOriginEventStore.EventStore.Serialization;

internal record InternalEvent(string Id, long Issued, int IssuedFraction,string ModelType,int ModelVersion,string Data)
{
    internal static InternalEvent From(EventModel model)
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
