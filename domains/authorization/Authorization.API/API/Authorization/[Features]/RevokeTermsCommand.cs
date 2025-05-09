using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class RevokeTermsCommand(Guid organizationId) : IRequest<RevokeTermsCommandResult>
{
    public Guid OrganizationId { get; } = organizationId;
}

public class RevokeTermsCommandResult;

public class RevokeTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeTermsCommand, RevokeTermsCommandResult>
{
    public async Task<RevokeTermsCommandResult> Handle(RevokeTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var organization = await organizationRepository.Query().FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization == null)
        {
            throw new EntityNotFoundException(request.OrganizationId, typeof(Organization));
        }

        organization.RevokeTerms();

        await unitOfWork.CommitAsync(cancellationToken);

        return new RevokeTermsCommandResult();
    }
}
