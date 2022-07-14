using EventStore.Serialization;

namespace EnergyOriginEventStore.Tests.Topics;

[EventModelVersion("Said", 1)]
public class Said : EventModel
{

    public string Actor { get; }
    public string Statement { get; }

    public Said(string actor, string statement)
    {
        Actor = actor;
        Statement = statement;
    }
}