using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;

namespace API.Authorization._Features_;

public class CreateOrganizationAndUserCommandHandler(
    IUnitOfWork unitOfWork,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IAffiliationRepository affiliationRepository)
    : IRequestHandler<CreateOrganizationAndUserCommand, CreateOrganizationAndUserCommandResult>
{
    public async Task<CreateOrganizationAndUserCommandResult> Handle(CreateOrganizationAndUserCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync();

        var organization = Organization.Create(Tin.Create(request.Tin), OrganizationName.Create(request.OrganizationName));
        organization.AcceptTerms(new Terms(request.TermsVersion));
        await organizationRepository.AddAsync(organization, cancellationToken);

        var user = User.Create(IdpUserId.Create(request.UserIdpId), UserName.Create(request.UserName));
        await userRepository.AddAsync(user, cancellationToken);

        var affiliation = Affiliation.Create(user, organization);
        await affiliationRepository.AddAsync(affiliation, cancellationToken);

        await unitOfWork.CommitAsync();

        return new CreateOrganizationAndUserCommandResult(organization.Id, user.Id, affiliation.UserId);
    }
}

public record CreateOrganizationAndUserCommand(
    string Tin,
    string OrganizationName,
    Guid UserIdpId,
    string UserName,
    string TermsVersion) : IRequest<CreateOrganizationAndUserCommandResult>;

public record CreateOrganizationAndUserCommandResult(
    Guid OrganizationId,
    Guid UserId,
    Guid AffiliationId
);
