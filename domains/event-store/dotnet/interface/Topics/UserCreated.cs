using System;
using EventStore;

namespace Topics;

public class UserCreated : EventModel {
    public string Type { get => "UserCreated"; }
    public int Version { get => 2; }
    public string Data { get => "JSON"; } // FIXME: json encoding

    public readonly string Id;
    public readonly string Subject;

    public UserCreated(string id, string subject) {
        Id = id;
        Subject = subject;
    }
}