using System;
using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;
using RegistryConnector.Worker.Cache;

namespace RegistryConnector.Worker.EventHandlers;

public class ProjectOriginRegistryEventHandler
{
    private readonly ILogger<ProjectOriginRegistryEventHandler> logger;
    private readonly ICertificateEventsInMemoryCache cache;
    private readonly IBus bus;

    public ProjectOriginRegistryEventHandler(ILogger<ProjectOriginRegistryEventHandler> logger, ICertificateEventsInMemoryCache cache, IBus bus)
    {
        this.logger = logger;
        this.cache = cache;
        this.bus = bus;
    }

    public async void OnRegistryEvents(CommandStatusEvent cse)
    {
        var commandId = HexHelper.ToHex(cse.Id);
        logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", commandId, cse.State, cse.Error);

        MessageWrapper<ProductionCertificateCreatedEvent>? createdEvent = null;
        var i = 0;
        while (createdEvent == null && i < 10)
        {
            createdEvent = cache.PopCertificateWithCommandId(cse.Id);
            i++;
            if (createdEvent != null) continue;

            logger.LogError("createEvent with id {id} was not found. Retry {i} / 10", commandId, i);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        if (createdEvent == null)
            return;

        if (cse.State == CommandState.Failed)
        {
            var rejectedEvent = new CertificateRejectedInProjectOriginEvent(createdEvent.Message.CertificateId, cse.Error!);
            await bus.Publish(rejectedEvent, createdEvent.SetIdsForOutgoingMessage);
            return;
        }

        var issuedInPoEvent = new CertificateIssuedInProjectOriginEvent(createdEvent.Message.CertificateId);

        await bus.Publish(issuedInPoEvent, createdEvent.SetIdsForOutgoingMessage);
    }
}
