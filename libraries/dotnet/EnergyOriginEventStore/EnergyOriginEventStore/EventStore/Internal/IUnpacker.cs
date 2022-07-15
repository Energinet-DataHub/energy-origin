using EventStore.Serialization;

namespace EventStore.Internal;

public interface IUnpacker
{
    InternalEvent UnpackEvent(string payload);

    EventModel UnpackModel(InternalEvent payload);

    T UnpackModel<T>(InternalEvent payload) where T : EventModel;
}
