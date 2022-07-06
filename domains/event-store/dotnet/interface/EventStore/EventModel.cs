using System;

namespace EventStore;

public interface EventModel {
    string Type { get; }
    int Version { get; }
    string Data { get; }
}

// FIXME: add serialize stuff to other? interface