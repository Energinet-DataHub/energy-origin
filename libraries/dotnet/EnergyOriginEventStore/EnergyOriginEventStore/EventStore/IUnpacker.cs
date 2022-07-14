using EventStore.Serialization;

namespace EventStore;

public interface IUnpacker
{
    Event UnpackEvent(string payload);

    EventModel UnpackModel(Event payload);

    T UnpackModel<T>(Event payload) where T : EventModel;
}
