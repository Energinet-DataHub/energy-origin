using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using OrganizationId = EnergyOrigin.Domain.ValueObjects.OrganizationId;

namespace API.Authorization._Features_.Internal;

public record CreateClientCommandHandler(IUnitOfWork UnitOfWork, IClientRepository ClientRepository, IOrganizationRepository OrganizationRepository) : IRequestHandler<CreateClientCommand, CreateClientCommandResult>
{
    public async Task<CreateClientCommandResult> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        await UnitOfWork.BeginTransactionAsync();
        var client = Client.Create(request.IdpClientId, request.Name, request.ClientType, request.RedirectUrl);
        var organization = Organization.Create(Tin.Empty(), OrganizationName.Create(request.Name.Value));
        client.SetOrganization(OrganizationId.Create(organization.Id));

        // Clients are manually vetted by EnergyTrackAndTrace, so we can assume that the organization has accepted the Service Provider Terms
        organization.AcceptServiceProviderTerms();
        await ClientRepository.AddAsync(client, cancellationToken);
        await OrganizationRepository.AddAsync(organization, cancellationToken);
        await UnitOfWork.CommitAsync();

        return new CreateClientCommandResult(client.Id, client.IdpClientId, client.Name, client.ClientType, client.RedirectUrl);
    }
}

public record CreateClientCommand(IdpClientId IdpClientId, ClientName Name, ClientType ClientType, string RedirectUrl) : IRequest<CreateClientCommandResult>;

public record CreateClientCommandResult(Guid Id, IdpClientId IdpClientId, ClientName Name, ClientType ClientType, string RedirectUrl);
