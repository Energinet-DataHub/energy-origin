using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Internal;

internal interface IUnpacker
{
    InternalEvent UnpackEvent(string payload);

    EventModel UnpackModel(InternalEvent payload);

    T UnpackModel<T>(InternalEvent payload) where T : EventModel;
}
