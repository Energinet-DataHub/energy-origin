using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Models;
using API.Repository;
using API.ValueObjects;
using API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;

namespace API.Authorization._Features_;

public record AcceptTermsCommand(string OrgCvr, string OrgName, Guid UserId) : IRequest<bool>;

public class AcceptTermsCommandHandler : IRequestHandler<AcceptTermsCommand, bool>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITermsRepository _termsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public AcceptTermsCommandHandler(
        IOrganizationRepository organizationRepository,
        ITermsRepository termsRepository,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _organizationRepository = organizationRepository;
        _termsRepository = termsRepository;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var usersOrganizationsCvr = Tin.Create(request.OrgCvr);

            var usersAffiliatedOrganization = await _organizationRepository.Query()
                .FirstOrDefaultAsync(o => o.Tin == usersOrganizationsCvr, cancellationToken);

            if (usersAffiliatedOrganization == null)
            {
                usersAffiliatedOrganization = Organization.Create(usersOrganizationsCvr, OrganizationName.Create(request.OrgName));
                await _organizationRepository.AddAsync(usersAffiliatedOrganization, cancellationToken);
            }

            var latestTerms = await _termsRepository.Query()
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestTerms == null)
            {
                return false;
            }

            if (!usersAffiliatedOrganization.TermsAccepted || usersAffiliatedOrganization.TermsVersion != latestTerms.Version)
            {
                usersAffiliatedOrganization.AcceptTerms(latestTerms);
            }

            await _unitOfWork.CommitAsync();

            await _publishEndpoint.Publish(new OrgAcceptedTerms(
                Guid.NewGuid(),
                Activity.Current?.Id ?? Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                usersAffiliatedOrganization.Id,
                request.OrgCvr,
                request.UserId
            ), cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
