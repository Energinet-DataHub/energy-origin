using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("Said", 1)]
public record Said(string Actor, string Statement) : EventModel;
