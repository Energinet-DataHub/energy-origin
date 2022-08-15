
namespace EnergyOriginEventStore.EventStore.Memory;

internal class MessageEventArgs : EventArgs
{
    public readonly string Message;
    public readonly string Topic;
    public readonly string Pointer;

    internal MessageEventArgs(string message, string topic, string pointer)
    {
        Message = message;
        Topic = topic;
        Pointer = pointer;
    }
}
