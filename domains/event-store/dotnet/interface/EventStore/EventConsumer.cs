using System;

namespace EventStore;

public interface IEventConsumer<T> { // can contain re-ordering mechanisms based on `Event.IssuedFraction`
    Task<T> Consume();
}
