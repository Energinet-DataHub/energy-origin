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

public class RevokeTermsCommand(Guid OrganizationId) : IRequest<RevokeTermsCommandResult>
{
    public Guid OrganizationId { get; } = OrganizationId;
}

public class RevokeTermsCommandResult();

public class RevokeTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    IUnitOfWork unitOfWork,
    ApplicationDbContext Context)
    : IRequestHandler<RevokeTermsCommand, RevokeTermsCommandResult>
{
    public async Task<RevokeTermsCommandResult> Handle(RevokeTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var orgs = await Context.Organizations.ToListAsync();

        var organization = await organizationRepository.Query().FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization == null)
        {
            throw new EntityNotFoundException(request.OrganizationId, typeof(Organization));
        }

        organization.RevokeTerms();

        await unitOfWork.CommitAsync();

        return new RevokeTermsCommandResult();
    }
}
