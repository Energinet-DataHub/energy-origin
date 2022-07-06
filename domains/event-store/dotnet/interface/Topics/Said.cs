using System;
using EventStore;

namespace Topics;

public class Said : EventModel {
    public string Type { get => "Said"; }
    public int Version { get => 1; }
    public string Data { get => "JSON"; } // FIXME: json encoding

    public readonly string Actor;
    public readonly string Statement;

    public Said(string actor, string statement) {
        Actor = actor;
        Statement = statement;
    }
}