namespace MessageRedeliveryPoc.MassTransit;

public record TestMessage2(int Number)
{
    public TestMessage2() : this(new Random().Next(0,1337))
    {
    }
}
