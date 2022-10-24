using EnergyOriginEventStore.EventStore.Serialization;

namespace Issuer.Worker;

[EventModelVersion("SomethingHappened", 1)]
public record SomethingHappened(string Foo) : EventModel;

[EventModelVersion("ThenThisHappened", 1)]
public record ThenThisHappened(string Bar) : EventModel;

