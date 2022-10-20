namespace EnergyOriginEventStore.EventStore.Serialization;

internal record InternalEvent(string Id, long Issued, long IssuedFraction, string ModelType, int ModelVersion, string Data)
{
    internal static InternalEvent From(EventModel model)
    {
        var now = DateTimeOffset.UtcNow;

        return new InternalEvent(
            Guid.NewGuid().ToString(),
            now.ToUnixTimeSeconds(),
            now.Ticks,
            model.Type,
            model.Version,
            model.Data
        );
    }
}
