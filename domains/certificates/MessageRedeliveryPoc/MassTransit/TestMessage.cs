namespace MessageRedeliveryPoc.MassTransit;

public record TestMessage(Guid Id)
{
    public TestMessage() : this(Guid.NewGuid())
    {
    }
};
