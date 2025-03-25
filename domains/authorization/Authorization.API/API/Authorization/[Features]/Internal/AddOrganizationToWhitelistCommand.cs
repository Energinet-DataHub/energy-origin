using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record AddOrganizationToWhitelistCommand(Tin Tin) : IRequest;

public class AddOrganizationToWhitelistCommandHandler : IRequestHandler<AddOrganizationToWhitelistCommand>
{
    private readonly IWhitelistedRepository _whitelistedRepository;

    public AddOrganizationToWhitelistCommandHandler(IWhitelistedRepository whitelistedRepository)
    {
        _whitelistedRepository = whitelistedRepository;
    }

    public async Task Handle(AddOrganizationToWhitelistCommand request, CancellationToken cancellationToken)
    {
        var existingEntry = await _whitelistedRepository.Query()
            .FirstOrDefaultAsync(w => w.Tin == request.Tin, cancellationToken);

        if (existingEntry == null)
        {
            var whitelisted = Whitelisted.Create(request.Tin);
            await _whitelistedRepository.AddAsync(whitelisted, cancellationToken);
        }
    }
}
