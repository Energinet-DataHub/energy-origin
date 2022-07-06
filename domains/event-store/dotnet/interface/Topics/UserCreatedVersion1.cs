using System;
using EventStore;

namespace Topics;

public class UserCreatedVersion1 : EventModel {
    public string Type { get => "UserCreated"; }
    public int Version { get => 1; }
    public string Data { get => "JSON"; } // FIXME: json encoding

    public readonly string Id;
    public readonly string Subject;

    public UserCreatedVersion1(string id, string subject) {
        Id = id;
        Subject = subject;
    }
}