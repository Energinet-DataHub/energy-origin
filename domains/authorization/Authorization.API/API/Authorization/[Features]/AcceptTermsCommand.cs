using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Models;
using API.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.IntegrationEvents.Events.Terms.V2;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.WalletClient;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization._Features_;

public record AcceptTermsCommand(string OrgCvr, string OrgName, Guid UserId) : IRequest;

public class AcceptTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    ITermsRepository termsRepository,
    IUnitOfWork unitOfWork,
    IWalletClient walletClient,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<AcceptTermsCommand>
{
    public async Task Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var usersOrganizationsCvr = Tin.Create(request.OrgCvr);

        var usersAffiliatedOrganization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == usersOrganizationsCvr, cancellationToken);

        if (usersAffiliatedOrganization == null)
        {
            usersAffiliatedOrganization =
                Organization.Create(usersOrganizationsCvr, OrganizationName.Create(request.OrgName));
            await organizationRepository.AddAsync(usersAffiliatedOrganization, cancellationToken);
        }

        var latestTerms = await termsRepository.Query()
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTerms == null)
        {
            throw new InvalidConfigurationException("No Terms configured");
        }

        if (!usersAffiliatedOrganization.TermsAccepted ||
            usersAffiliatedOrganization.TermsVersion != latestTerms.Version)
        {
            usersAffiliatedOrganization.AcceptTerms(latestTerms);
        }

        await EnsureWalletExistsAsync(usersAffiliatedOrganization.Id);

        await publishEndpoint.Publish(new OrgAcceptedTerms(
            Guid.NewGuid(),
            Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            usersAffiliatedOrganization.Id,
            request.OrgCvr,
            request.UserId
        ), cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task EnsureWalletExistsAsync(Guid organizationId)
    {
        var wallets = await walletClient.GetWallets(organizationId, CancellationToken.None);
        var wallet = wallets.Result.FirstOrDefault();

        if (wallet is null)
        {
            var createWalletResponse = await walletClient.CreateWallet(organizationId, CancellationToken.None);
            if (createWalletResponse == null)
                throw new WalletNotCreated("Failed to create wallet.");
            return;
        }

        var enableWalletResponse = walletClient.EnableWallet(wallet.Id, organizationId, CancellationToken.None);
        if (enableWalletResponse == null)
            throw new BusinessException("Failed to create wallet.");
    }
}
