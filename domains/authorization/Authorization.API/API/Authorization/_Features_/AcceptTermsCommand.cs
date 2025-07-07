using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Exceptions;
using API.Data;
using API.Events;
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

public record AcceptTermsCommand(
    string OrgCvr,
    string OrgName,
    Guid UserId,
    bool IsTrial) : IRequest;
public class AcceptTermsCommandHandler(
    IOrganizationRepository organizationRepository,
    ITermsRepository termsRepository,
    IWhitelistedRepository whitelistedRepository,
    IUnitOfWork unitOfWork,
    IWalletClient walletClient,
    IPublishEndpoint publishEndpoint,
    IDomainEventDispatcher domainEventDispatcher)
    : IRequestHandler<AcceptTermsCommand>
{
    public async Task Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var tin = Tin.Create(request.OrgCvr);
        var organizationName = OrganizationName.Create(request.OrgName);

        var isWhitelisted = await whitelistedRepository.Query()
            .AnyAsync(w => w.Tin == tin, cancellationToken);

        var organization = await organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == tin, cancellationToken);

        if (organization == null)
        {
            organization = isWhitelisted
                ? Organization.Create(tin, organizationName)
                : Organization.CreateTrial(tin, organizationName);

            await organizationRepository.AddAsync(organization, cancellationToken);
        }

        var termsType = request.IsTrial ? TermsType.Trial : TermsType.Normal;

        var latestTerms = await termsRepository.Query()
            .Where(t => t.Type == termsType)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTerms is null)
            throw new InvalidConfigurationException($"No {termsType} terms configured");

        if (!organization.TermsAccepted || organization.TermsVersion != latestTerms.Version)
        {
            organization.AcceptTerms(latestTerms, isWhitelisted);
        }

        await EnsureWalletExistsAsync(organization.Id);

        await domainEventDispatcher.DispatchAsync(organization.DomainEvents);

        await publishEndpoint.Publish(new OrgAcceptedTerms(
            Guid.NewGuid(),
            Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            organization.Id,
            request.OrgCvr,
            request.UserId
        ), cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        organization.ClearDomainEvents();
    }

    private async Task EnsureWalletExistsAsync(Guid organizationId)
    {
        var wallets = await walletClient.GetWalletsAsync(organizationId, CancellationToken.None);
        var wallet = wallets.Result.FirstOrDefault();

        if (wallet is null)
        {
            var createWalletResponse = await walletClient.CreateWalletAsync(organizationId, CancellationToken.None);
            if (createWalletResponse == null)
                throw new WalletNotCreated("Failed to create wallet.");
            return;
        }

        var enableWalletResponse = walletClient.EnableWalletAsync(wallet.Id, organizationId, CancellationToken.None);
        if (enableWalletResponse == null)
            throw new BusinessException("Failed to create wallet.");
    }
}
