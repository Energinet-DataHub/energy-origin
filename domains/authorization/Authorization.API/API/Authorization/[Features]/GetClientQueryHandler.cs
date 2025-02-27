using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;
public class GetClientQueryHandler(IClientRepository clientRepository)
    : IRequestHandler<GetClientQuery, GetClientQueryResult>
{
    public async Task<GetClientQueryResult> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        var client = await clientRepository
                         .Query()
                         .FirstOrDefaultAsync(client => client.IdpClientId == request.IdpClientId,
                             cancellationToken: cancellationToken) ??
                     throw new EntityNotFoundException(request.IdpClientId.Value, typeof(Client));
        return new GetClientQueryResult(client.IdpClientId, client.Name, client.RedirectUrl);
    }
}
public record GetClientQuery(IdpClientId IdpClientId) : IRequest<GetClientQueryResult>;
public record GetClientQueryResult(IdpClientId IdpClientId, ClientName Name, string RedirectUrl);
