using EnergyOriginEventStore.EventStore.Serialization;

namespace CertificateEvents;

public abstract record MeasurementBaseEvent : EventModel
{
    public abstract string GSRN { get; init; }
}
