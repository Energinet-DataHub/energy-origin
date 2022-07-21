using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("UserCreated", 2)]
public record UserCreatedVersion2(string Id, string Subject, string NickName) : EventModel;
