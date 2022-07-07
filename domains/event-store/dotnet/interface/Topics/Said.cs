using System;
using EventStore;
using EventStore.Serialization;

namespace Topics;

public class Said : EventModel {
    public override string Type { get => "Said"; }
    public override int Version { get => 1; }

    public string Actor { get; }
    public string Statement { get; }

    public Said(string actor, string statement) {
        Actor = actor;
        Statement = statement;
    }
}