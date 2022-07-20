namespace EnergyOriginEventStore.EventStore.Serialization;

public class Event<T> where T : EventModel
{
    public T EventModel { get; }

    public string Pointer { get; }

    internal Event(T eventModel, string pointer)
    {
        EventModel = eventModel;
        Pointer = pointer;
    }
}