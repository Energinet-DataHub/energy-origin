using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Authorization._Features_.Internal;

public class AddOrganizationToWhitelistCommandHandler(
    IWhitelistedRepository whitelistedRepository,
    IOrganizationRepository organizationRepository,
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

        var org = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == request.Tin, cancellationToken: cancellationToken);

        org?.InvalidateTerms();

        await unitOfWork.CommitAsync();
    }
}

public record AddOrganizationToWhitelistCommand(Tin Tin) : IRequest;
