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

public class AcceptTermsCommandHandler : IRequestHandler<AcceptTermsCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITermsRepository _termsRepository;
    private readonly IWhitelistedRepository _whitelistedRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletClient _walletClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public AcceptTermsCommandHandler(
        IOrganizationRepository organizationRepository,
        ITermsRepository termsRepository,
        IWhitelistedRepository whitelistedRepository,
        IUnitOfWork unitOfWork,
        IWalletClient walletClient,
        IPublishEndpoint publishEndpoint)
    {
        _organizationRepository = organizationRepository;
        _termsRepository = termsRepository;
        _whitelistedRepository = whitelistedRepository;
        _unitOfWork = unitOfWork;
        _walletClient = walletClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(AcceptTermsCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var tin = Tin.Create(request.OrgCvr);
        var organizationName = OrganizationName.Create(request.OrgName);

        var isWhitelisted = await _whitelistedRepository.Query()
            .AnyAsync(w => w.Tin == tin, cancellationToken);

        var organization = await _organizationRepository.Query()
            .FirstOrDefaultAsync(o => o.Tin == tin, cancellationToken);

        if (organization == null)
        {
            organization = isWhitelisted
                ? Organization.Create(tin, organizationName)
                : Organization.CreateTrial(tin, organizationName);

            await _organizationRepository.AddAsync(organization, cancellationToken);
        }

        var latestTerms = await _termsRepository.Query()
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTerms == null)
            throw new InvalidConfigurationException("No Terms configured");

        if (!organization.TermsAccepted || organization.TermsVersion != latestTerms.Version)
        {
            organization.AcceptTerms(latestTerms, isWhitelisted);
        }

        await EnsureWalletExistsAsync(organization.Id, cancellationToken);

        await _publishEndpoint.Publish(new OrgAcceptedTerms(
            Guid.NewGuid(),
            Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            organization.Id,
            request.OrgCvr,
            request.UserId
        ), cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task EnsureWalletExistsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var wallets = await _walletClient.GetWallets(organizationId, cancellationToken);
        var wallet = wallets.Result.FirstOrDefault();

        if (wallet is null)
        {
            var createWalletResponse = await _walletClient.CreateWallet(organizationId, cancellationToken);
            if (createWalletResponse == null)
                throw new WalletNotCreated("Failed to create wallet.");
            return;
        }

        var enableWalletResponse = await _walletClient.EnableWallet(wallet.Id, organizationId, cancellationToken);
        if (enableWalletResponse == null)
            throw new BusinessException("Failed to enable wallet.");
    }
}
