using System;
using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;
using ProjectOrigin.Electricity.Client.Models;
using RegistryConnector.Worker.Cache;

namespace RegistryConnector.Worker.Application.PoEventHandlers;

public class PoRegistryEventHandler
{
    private readonly ILogger<PoRegistryEventHandler> logger;
    private readonly ICertificateEventsInMemoryCache cache;
    private readonly IBus bus;

    public PoRegistryEventHandler(ILogger<PoRegistryEventHandler> logger, ICertificateEventsInMemoryCache cache, IBus bus)
    {
        this.logger = logger;
        this.cache = cache;
        this.bus = bus;
    }

    public async void OnRegistryEvents(CommandStatusEvent cse)
    {
        logger.LogInformation("Received event. Id={id}, State={state}, Error={error}", HexHelper.ToHex(cse.Id), cse.State, cse.Error);

        var createdEvent = cache.PopCertificateWithCommandId(cse.Id);

        if (createdEvent == null)
        {
            logger.LogError("createEvent with id {id} was not found.", HexHelper.ToHex(cse.Id));
            return;
        }

        if (cse.State == CommandState.Failed)
        {
            var rejectedEvent = new CertificateRejectedInPoEvent(createdEvent.Message.CertificateId, cse.Error);
            await bus.Publish(rejectedEvent, createdEvent.SetIdsForOutgoingMessage);
            return;
        }

        var issuedInPoEvent = new CertificateIssuedInPoEvent(createdEvent.Message.CertificateId);

        await bus.Publish(issuedInPoEvent, createdEvent.SetIdsForOutgoingMessage);
    }
}
