using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public class GrantConsentCommandHandler : IRequestHandler<GrantConsentCommand>
{
    private readonly IClientRepository _clientRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IConsentRepository _consentRepository;
    private readonly IAffiliationRepository _affiliationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GrantConsentCommandHandler(IClientRepository clientRepository, IOrganizationRepository organizationRepository,
        IConsentRepository consentRepository, IAffiliationRepository affiliationRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _organizationRepository = organizationRepository;
        _consentRepository = consentRepository;
        _affiliationRepository = affiliationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(GrantConsentCommand command, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetAsync(command.organizationId, cancellationToken);

        var affiliation = await _affiliationRepository.Query().Where(a => a.UserId == command.userId && a.OrganizationId == command.organizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (affiliation is null)
        {
            throw new UserNotAffiliatedWithOrganizationCommandException();
        }

        var client = await _clientRepository.GetAsync(command.clientId, cancellationToken);

        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await _consentRepository.AddAsync(consent, cancellationToken);

        await _unitOfWork.CommitAsync();
    }
}

public record GrantConsentCommand(Guid userId, Guid organizationId, Guid clientId) : IRequest;

public class UserNotAffiliatedWithOrganizationCommandException() : ForbiddenException("Not authorized to perform action");
