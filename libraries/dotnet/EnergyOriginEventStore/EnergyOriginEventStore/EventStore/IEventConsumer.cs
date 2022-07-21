namespace EnergyOriginEventStore.EventStore;

public interface IEventConsumer : IDisposable
{
    // can contain re-ordering mechanisms based on `Event.IssuedFraction`

}
