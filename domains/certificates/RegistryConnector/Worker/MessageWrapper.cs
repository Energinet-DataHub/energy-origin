using System;
using MassTransit;

namespace RegistryConnector.Worker;

public class MessageWrapper<TIncoming> where TIncoming : class
{
    private readonly Guid? correlationId;
    private readonly Guid? conversationId;

    public MessageWrapper(ConsumeContext<TIncoming> context)
    {
        Message = context.Message;
        correlationId = context.CorrelationId;
        conversationId = context.ConversationId;
    }

    public MessageWrapper(TIncoming message, Guid correlationId, Guid conversationId)
    {
        Message = message;
        this.correlationId = correlationId;
        this.conversationId = conversationId;
    }

    public TIncoming Message { get; }

    public void SetIdsForOutgoingMessage<TOutgoing>(PublishContext<TOutgoing> ctx) where TOutgoing : class
    {
        // Ids set based on description from https://masstransit.io/documentation/concepts/messages#message-correlation
        ctx.ConversationId = conversationId;
        ctx.InitiatorId = correlationId;
    }
}