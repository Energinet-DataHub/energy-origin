using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;

namespace API.Authorization._Features_;

public record CreateClientCommandHandler(IUnitOfWork UnitOfWork, IClientRepository ClientRepository) : IRequestHandler<CreateClientCommand, CreateClientCommandResult>
{
    public async Task<CreateClientCommandResult> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        await UnitOfWork.BeginTransactionAsync();
        await ClientRepository.AddAsync(Client.Create(request.IdpClientId, request.Name, request.ClientType, request.RedirectUrl), cancellationToken);
        await UnitOfWork.CommitAsync();

        return new CreateClientCommandResult();
    }
}

public record CreateClientCommand(IdpClientId IdpClientId, ClientName Name, ClientType ClientType, string RedirectUrl)
    : IRequest<CreateClientCommandResult>;

public record CreateClientCommandResult();
