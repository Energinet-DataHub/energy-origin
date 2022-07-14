using EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("UserCreated", 1)]
public class UserCreatedVersion1 : EventModel {

    public string Id { get; }
    public string Subject { get; }

    public UserCreatedVersion1(string id, string subject) {
        Id = id;
        Subject = subject;
    }
}
