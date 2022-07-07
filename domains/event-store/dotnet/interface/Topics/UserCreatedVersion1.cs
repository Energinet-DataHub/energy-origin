using System;
using System.Text.Json;
using EventStore;
using EventStore.Serialization;

namespace Topics;

public class UserCreatedVersion1 : EventModel {
    public override string Type { get => "UserCreated"; }
    public override int Version { get => 1; }

    public string Id { get; }
    public string Subject { get; }

    public UserCreatedVersion1(string id, string subject) {
        Id = id;
        Subject = subject;
    }
}
