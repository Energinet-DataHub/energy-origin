
namespace EnergyOriginEventStore.EventStore.Memory;

public class MessageEventArgs : EventArgs
{
    public readonly string Message;
    public readonly string Topic;
    public readonly string Pointer;

    public MessageEventArgs(string message, string topic, string pointer)
    {
        Message = message;
        Topic = topic;
        Pointer = pointer;
    }
}
