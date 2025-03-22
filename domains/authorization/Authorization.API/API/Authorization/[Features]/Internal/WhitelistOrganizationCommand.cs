using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public record WhitelistOrganizationCommand(Tin Tin) : IRequest;

public class WhitelistOrganizationCommandHandler : IRequestHandler<WhitelistOrganizationCommand>
{
    private readonly IWhitelistedRepository _whitelistedRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WhitelistOrganizationCommandHandler(IWhitelistedRepository whitelistedRepository, IUnitOfWork unitOfWork)
    {
        _whitelistedRepository = whitelistedRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(WhitelistOrganizationCommand request, CancellationToken cancellationToken)
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
