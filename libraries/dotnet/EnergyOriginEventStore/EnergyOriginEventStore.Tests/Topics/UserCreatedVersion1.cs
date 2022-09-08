using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("UserCreated", 1)]
public record UserCreatedVersion1(string Id, string Subject) : EventModel
{
    public override EventModel? NextVersion() => new UserCreatedVersion2(Id, Subject, "Anonymous");
}
