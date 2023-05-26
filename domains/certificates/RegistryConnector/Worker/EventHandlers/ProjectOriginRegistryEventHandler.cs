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

    public async Task OnRegistryEvents(CommandStatusEvent cse)
    {
        var commandId = cse.Id.ToHex();
        logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", commandId, cse.State, cse.Error);

        var createdEvent = await GetFromCache(cse, commandId);

        if (createdEvent == null)
            return;

        if (cse.State == CommandState.Failed)
        {
            var rejectedEvent = new CertificateRejectedInProjectOriginEvent(createdEvent.Message.CertificateId, cse.Error ?? "No error reported from Project Origin registry.");
            await bus.Publish(rejectedEvent, createdEvent.SetIdsForOutgoingMessage);
            return;
        }

        var issuedInPoEvent = new CertificateIssuedInProjectOriginEvent(createdEvent.Message.CertificateId);

        await bus.Publish(issuedInPoEvent, createdEvent.SetIdsForOutgoingMessage);
    }

    private async Task<MessageWrapper<ProductionCertificateCreatedEvent>?> GetFromCache(CommandStatusEvent cse, string commandId)
    {
        for (var i = 1; i < 11; i++)
        {
            var createdEvent = cache.PopCertificateWithCommandId(cse.Id);
            if (createdEvent != null) return createdEvent;

            logger.LogWarning("createEvent with id {id} was not found. Retry {i} / 10", commandId, i);
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return null;
    }
}
