using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_.Internal;

public class AddOrganizationToWhitelistCommandHandler(
    IWhitelistedRepository whitelistedRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddOrganizationToWhitelistCommand>
{
    public async Task Handle(AddOrganizationToWhitelistCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var checkIfOrganizationIsAlreadyWhitelisted = await whitelistedRepository.Query()
            .FirstOrDefaultAsync(w => w.Tin == request.Tin, cancellationToken);

        if (checkIfOrganizationIsAlreadyWhitelisted == null)
        {
            var whitelisted = Whitelisted.Create(request.Tin);
            await whitelistedRepository.AddAsync(whitelisted, cancellationToken);
        }

        await unitOfWork.CommitAsync();
    }
}

public record AddOrganizationToWhitelistCommand(Tin Tin) : IRequest;

