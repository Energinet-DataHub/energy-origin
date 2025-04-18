using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.UnitOfWork;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.ContractService.Internal;

public class RemoveOrganizationContractsAndSlidingWindowsCommand(Guid organizationId) : IRequest<RemoveOrganizationContractsAndSlidingWindowsCommandResult>
{
    public OrganizationId OrganizationId { get; init; } = OrganizationId.Create(organizationId);
}

public class RemoveOrganizationContractsAndSlidingWindowsCommandResult
{
}

public class RemoveOrganizationContractsAndSlidingWindowsCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveOrganizationContractsAndSlidingWindowsCommand, RemoveOrganizationContractsAndSlidingWindowsCommandResult>
{
    public async Task<RemoveOrganizationContractsAndSlidingWindowsCommandResult> Handle(
        RemoveOrganizationContractsAndSlidingWindowsCommand request,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var organizationId = request.OrganizationId.Value.ToString();

        var contracts = await unitOfWork.CertificateIssuingContractRepo
            .GetAllMeteringPointOwnerContracts(organizationId, cancellationToken);

        var gsrns = contracts.Select(c => c.GSRN).Distinct().ToList();

        var slidingWindows = await unitOfWork.SlidingWindowState.Query()
            .Where(sw => gsrns.Contains(sw.GSRN))
            .ToListAsync(cancellationToken);

        if (contracts.Count > 0)
        {
            unitOfWork.CertificateIssuingContractRepo.RemoveRange(contracts);
        }

        if (slidingWindows.Count > 0)
        {
            unitOfWork.SlidingWindowState.RemoveRange(slidingWindows);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return new RemoveOrganizationContractsAndSlidingWindowsCommandResult();
    }
}
