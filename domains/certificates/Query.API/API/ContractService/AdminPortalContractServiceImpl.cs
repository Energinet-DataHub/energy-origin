using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.Query.API.ApiModels.Requests.Internal;
using API.UnitOfWork;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WalletClient;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;
using Technology = DataContext.ValueObjects.Technology;

namespace API.ContractService;

internal class AdminPortalContractServiceImpl(
    IMeteringPointsClient meteringPointsClient,
    IWalletClient walletClient,
    IStampClient stampClient,
    IUnitOfWork unitOfWork,
    ILogger<ContractServiceImpl> logger) : IAdminPortalContractService
{
    public async Task<CreateContractResult> Create(
            CreateContracts contracts,
            CancellationToken cancellationToken)
    {
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(contracts.MeteringPointOwnerId.ToString(), cancellationToken);
        var contractsByGsrn =
            await GetAllContractsByGsrn([.. contracts.Contracts.Select(c => c.GSRN)], cancellationToken);

        var newContracts = new List<CertificateIssuingContract>();
        var number = 0;
        foreach (var contract in contracts.Contracts)
        {
            var startDate = DateTimeOffset.FromUnixTimeSeconds(contract.StartDate);
            DateTimeOffset? endDate = contract.EndDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(contract.EndDate.Value)
                : null;

            var matchingMeteringPoint = meteringPoints?.Result.Find(mp => mp.Gsrn == contract.GSRN);

            if (matchingMeteringPoint == null)
            {
                return new GsrnNotFound(contract.GSRN);
            }

            if (!matchingMeteringPoint.CanBeUsedForIssuingCertificates)
            {
                return new CannotBeUsedForIssuingCertificates(contract.GSRN);
            }

            var contractsGsrn = contractsByGsrn.Where(c => c.GSRN == contract.GSRN).ToList();

            var overlappingContract = contractsGsrn.Find(c =>
                c.Overlaps(startDate, endDate));

            if (overlappingContract != null)
            {
                return new ContractAlreadyExists(overlappingContract);
            }

            var wallets = await walletClient.GetWalletsAsync(contracts.MeteringPointOwnerId, cancellationToken);

            var walletId = wallets.Result.First().Id;

            var contractNumber = contractsGsrn.Count != 0
                ? contractsGsrn.Max(c => c.ContractNumber) + number + 1
                : number;

            var walletEndpoint =
                await walletClient.CreateWalletEndpointAsync(walletId, contracts.MeteringPointOwnerId,
                    cancellationToken);

            var recipientResponse = await stampClient.CreateRecipient(walletEndpoint, cancellationToken);

            var sponsorshipEndDate = await unitOfWork.SponsorshipRepo
                .GetEndDateAsync(new Gsrn(contract.GSRN), cancellationToken);


            var issuingContract = CertificateIssuingContract.Create(
                contractNumber,
                new Gsrn(contract.GSRN),
                matchingMeteringPoint.GridArea,
                Map(matchingMeteringPoint.MeteringPointType),
                contracts.MeteringPointOwnerId.ToString(),
                startDate,
                endDate,
                recipientResponse.Id,
                Map(matchingMeteringPoint.MeteringPointType, matchingMeteringPoint.Technology),
                contracts.IsTrial,
                sponsorshipEndDate);

            newContracts.Add(issuingContract);
            contractsByGsrn.Add(issuingContract);

            // We don't want to log the ActorId and ActorName, since in this scenario it would be of the supporter logged in to the Admin Portal
            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
                actorId: Guid.Empty,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: string.Empty,
                organizationTin: contracts.OrganizationTin,
                organizationName: contracts.OrganizationName,
                otherOrganizationTin: string.Empty,
                otherOrganizationName: string.Empty,
                entityType: ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                actionType: ActivityLogEntry.ActionTypeEnum.Activated,
                entityId: contract.GSRN)
            );
            number++;
        }

        try
        {
            await unitOfWork.CertificateIssuingContractRepo.SaveRange(newContracts);

            await unitOfWork.SaveAsync(cancellationToken);

            return new CreateContractResult.Success(newContracts);
        }
        catch (DbUpdateException)
        {
            return new ContractAlreadyExists(null);
        }
    }

    private async Task<List<CertificateIssuingContract>> GetAllContractsByGsrn(List<string> gsrn,
        CancellationToken cancellationToken)
    {
        var contracts =
            await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(gsrn,
                cancellationToken);
        return [.. contracts];
    }

    private static MeteringPointType Map(MeterType type)
    {
        if (type == MeterType.Production) return MeteringPointType.Production;
        if (type == MeterType.Consumption) return MeteringPointType.Consumption;

        throw new ArgumentException($"Unsupported MeterType {type}");
    }

    private static Technology? Map(MeterType meterType, Clients.Technology technology)
    {
        if (meterType == MeterType.Production)
        {
            return new Technology(technology.AibFuelCode, technology.AibTechCode);
        }

        return null;
    }
}
