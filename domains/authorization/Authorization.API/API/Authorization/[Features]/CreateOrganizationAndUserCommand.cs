using System;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Models;
using API.Repository;
using API.ValueObjects;
using MediatR;

namespace API.Authorization._Features_;

public class CreateOrganizationAndUserCommandHandler : IRequestHandler<CreateOrganizationAndUserCommand, CreateOrganizationAndUserCommandResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAffiliationRepository _affiliationRepository;
    private readonly ITermsRepository _termsRepository;

    public CreateOrganizationAndUserCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IAffiliationRepository affiliationRepository,
        ITermsRepository termsRepository)
    {
        _unitOfWork = unitOfWork;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _affiliationRepository = affiliationRepository;
        _termsRepository = termsRepository;
    }

    public async Task<CreateOrganizationAndUserCommandResult> Handle(CreateOrganizationAndUserCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();

        var terms = await _termsRepository.GetByVersionAsync(request.TermsVersion, cancellationToken);
        if (terms == null)
        {
            terms = new Terms(request.TermsVersion, "Sample terms text.");
            await _termsRepository.AddAsync(terms, cancellationToken);
        }

        var organization = Organization.Create(new Tin(request.Tin), new OrganizationName(request.OrganizationName));
        organization.AcceptTerms(terms);
        await _organizationRepository.AddAsync(organization, cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            IdpUserId = request.UserIdpId,
            Name = new UserName(request.UserName)
        };
        await _userRepository.AddAsync(user, cancellationToken);

        var affiliation = new Affiliation
        {
            UserId = user.Id,
            OrganizationId = organization.Id
        };
        await _affiliationRepository.AddAsync(affiliation, cancellationToken);

        await _unitOfWork.CommitAsync();

        return new CreateOrganizationAndUserCommandResult(organization.Id, user.Id, affiliation.Id);
    }
}

public record CreateOrganizationAndUserCommand(
    string Tin,
    string OrganizationName,
    string UserIdpId,
    string UserName,
    string TermsVersion) : IRequest<CreateOrganizationAndUserCommandResult>;

public record CreateOrganizationAndUserCommandResult(
    Guid OrganizationId,
    Guid UserId,
    Guid AffiliationId);
