
namespace EnergyOriginEventStore.EventStore.Memory;

internal class MessageEventArgs : EventArgs
{
    public readonly string Message;
    public readonly string Topic;
    public readonly MemoryPointer Pointer;

    internal MessageEventArgs(string message, string topic, MemoryPointer pointer)
    {
        Message = message;
        Topic = topic;
        Pointer = pointer;
    }
}
