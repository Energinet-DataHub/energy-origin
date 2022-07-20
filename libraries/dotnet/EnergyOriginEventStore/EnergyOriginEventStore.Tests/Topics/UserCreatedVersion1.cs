using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("UserCreated", 1)]
public class UserCreatedVersion1 : EventModel
{
    public string Id { get; }
    public string Subject { get; }

    public UserCreatedVersion1(string id, string subject)
    {
        Id = id;
        Subject = subject;
    }

    public override EventModel? NextVersion()
    {
        return new UserCreatedVersion2(Id, Subject, "Anonymous");
    }
}
