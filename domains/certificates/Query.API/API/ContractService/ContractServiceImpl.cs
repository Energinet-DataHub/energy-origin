using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.Query.API.ApiModels.Requests;
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

internal class ContractServiceImpl : IContractService
{
    private readonly IMeteringPointsClient meteringPointsClient;
    private readonly IWalletClient walletClient;
    private readonly IStampClient stampClient;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<ContractServiceImpl> logger;

    public ContractServiceImpl(
        IMeteringPointsClient meteringPointsClient,
        IWalletClient walletClient,
        IStampClient stampClient,
        IUnitOfWork unitOfWork,
        ILogger<ContractServiceImpl> logger)
    {
        this.meteringPointsClient = meteringPointsClient;
        this.walletClient = walletClient;
        this.stampClient = stampClient;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CreateContractResult> Create(CreateContracts contracts, Guid meteringPointOwnerId, Guid subjectId, string subjectName, string organizationName,
        string organizationTin, CancellationToken cancellationToken)
    {
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(meteringPointOwnerId.ToString(), cancellationToken);
        var contractsByGsrn =
            await GetAllContractsByGsrn(contracts.Contracts.Select(c => c.GSRN).ToList(), cancellationToken);

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

            var wallets = await walletClient.GetWallets(meteringPointOwnerId, cancellationToken);

            var walletId = wallets.Result.First().Id;

            var contractNumber = contractsGsrn.Any()
                ? contractsGsrn.Max(c => c.ContractNumber) + number + 1
                : number;

            var walletEndpoint =
                await walletClient.CreateWalletEndpoint(walletId, meteringPointOwnerId,
                    cancellationToken);

            var recipientResponse = await stampClient.CreateRecipient(walletEndpoint, cancellationToken);

            var issuingContract = CertificateIssuingContract.Create(
                contractNumber,
                new Gsrn(contract.GSRN),
                matchingMeteringPoint.GridArea,
                Map(matchingMeteringPoint.Type),
                meteringPointOwnerId.ToString(),
                startDate,
                endDate,
                recipientResponse.Id,
                Map(matchingMeteringPoint.Type, matchingMeteringPoint.Technology));

            newContracts.Add(issuingContract);
            contractsByGsrn.Add(issuingContract);

            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
                actorId: subjectId,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: subjectName,
                organizationTin: organizationTin,
                organizationName: organizationName,
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

            await unitOfWork.SaveAsync();

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
        return contracts.ToList();
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

    public async Task<SetEndDateResult> SetEndDate(EditContracts contracts,
        Guid meteringPointOwnerId, Guid subjectId, string subjectName, string organizationName,
        string organizationTin, CancellationToken cancellationToken)
    {
        var meteringPointOwner = meteringPointOwnerId.ToString();
        var issuingContracts =
            await unitOfWork.CertificateIssuingContractRepo.GetAllByIds(contracts.Contracts.Select(c => c.Id).ToList(),
                cancellationToken);

        var contractsByGsrn =
            await GetAllContractsByGsrn(issuingContracts.Select(c => c.GSRN).ToList(), cancellationToken);

        foreach (var updatedContract in contracts.Contracts)
        {
            DateTimeOffset? newEndDate = updatedContract.EndDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(updatedContract.EndDate.Value)
                : null;
            var existingContract = issuingContracts.Find(c => c.Id == updatedContract.Id);
            if (existingContract == null)
            {
                logger.LogInformation("Non existing contracts for {owner} contractId: {id}", meteringPointOwner,
                    updatedContract.Id);

                return new NonExistingContract();
            }

            if (existingContract.MeteringPointOwner != meteringPointOwner)
            {
                logger.LogInformation("Metering point owner no match for {owner}", meteringPointOwner);
                return new MeteringPointOwnerNoMatch();
            }

            if (newEndDate != null &&
                newEndDate <= existingContract.StartDate)
            {
                logger.LogInformation("End date before start date. EndDate: {enddate}, StartDate: {startDate}",
                    newEndDate, existingContract.StartDate);
                return new EndDateBeforeStartDate(existingContract.StartDate, newEndDate.Value);
            }

            var overlappingContract = contractsByGsrn.Where(c => c.GSRN == existingContract.GSRN).FirstOrDefault(c =>
                c.Overlaps(existingContract.StartDate, newEndDate) &&
                c.Id != existingContract.Id);

            if (overlappingContract != null)
            {
                logger.LogInformation("Overlapping: {enddate}, StartDate: {startDate}", newEndDate,
                    existingContract.StartDate);
                return new OverlappingContract();
            }

            existingContract.EndDate = newEndDate;

            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
                actorId: subjectId,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: subjectName,
                organizationTin: organizationTin,
                organizationName: organizationName,
                otherOrganizationTin: string.Empty,
                otherOrganizationName: string.Empty,
                entityType: ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
                entityId: existingContract.GSRN)
            );
        }

        unitOfWork.CertificateIssuingContractRepo.UpdateRange(issuingContracts);
        await unitOfWork.SaveAsync();
        return new SetEndDateResult.Success();
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(Guid meteringPointOwnerId,
        CancellationToken cancellationToken)
        => unitOfWork.CertificateIssuingContractRepo.GetAllMeteringPointOwnerContracts(meteringPointOwnerId.ToString(),
            cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, IList<Guid> authorizedMeteringPointOwnerIds, CancellationToken cancellationToken)
    {
        var contract = await unitOfWork.CertificateIssuingContractRepo.GetById(id, cancellationToken);

        if (contract == null)
        {
            return null;
        }

        return authorizedMeteringPointOwnerIds.Select(x => x.ToString()).Contains(contract.MeteringPointOwner.Trim())
            ? contract
            : null;
    }
}
