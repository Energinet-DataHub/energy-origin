using System;
using System.Text.Json;
using EventStore;
using EventStore.Serialization;

namespace Topics;

public class UserCreated : EventModel {
    public override string Type { get => "UserCreated"; }
    public override int Version { get => 2; }

    public string Id { get; }
    public string Subject { get; }
    public string NickName { get; }

    public UserCreated(string id, string subject, string nickName) {
        Id = id;
        Subject = subject;
        NickName = nickName;
    }
}
