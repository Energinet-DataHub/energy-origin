using EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("UserCreated", 2)]
public class UserCreated : EventModel
{

    public string Id { get; }
    public string Subject { get; }
    public string NickName { get; }

    public UserCreated(string id, string subject, string nickName)
    {
        Id = id;
        Subject = subject;
        NickName = nickName;
    }
}
