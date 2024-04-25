using API.ContractService.Clients;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using API.UnitOfWork;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;
using Technology = DataContext.ValueObjects.Technology;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.IdentityModel.Tokens;
using ProjectOriginClients;

namespace API.ContractService;

internal class ContractServiceImpl : IContractService
{
    private readonly IMeteringPointsClient meteringPointsClient;
    private readonly IWalletClient walletClient;
    private readonly IUnitOfWork unitOfWork;

    public ContractServiceImpl(
        IMeteringPointsClient meteringPointsClient,
        IWalletClient walletClient,
        IUnitOfWork unitOfWork)
    {
        this.meteringPointsClient = meteringPointsClient;
        this.walletClient = walletClient;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateContractResult> Create(CreateContracts contracts, UserDescriptor user,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = user.Subject.ToString();
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(meteringPointOwner, cancellationToken);
        var contractsByGsrn =
            await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(contracts.Contracts.Select(c => c.GSRN).ToList(),
                cancellationToken);
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
                return new GsrnNotFound();
            }

            var overlappingContract = contractsByGsrn.FirstOrDefault(c =>
                c.Overlaps(startDate, endDate.Value));
            if (overlappingContract != null)
            {
                return new ContractAlreadyExists(overlappingContract);
            }

            var wallets = await walletClient.GetWallets(user.Subject.ToString(), cancellationToken);

            var walletId = wallets.Result.FirstOrDefault()?.Id;
            if (walletId == null)
            {
                var createWalletResponse = await walletClient.CreateWallet(user.Subject.ToString(), cancellationToken);

                if (createWalletResponse == null)
                    throw new ApplicationException("Failed to create wallet.");

                walletId = createWalletResponse.WalletId;
            }

            var contractNumber = contractsByGsrn.Any()
                    ? contractsByGsrn.Max(c => c.ContractNumber) + number + 1
                    : number;

            var walletDepositEndpoint =
                await walletClient.CreateWalletEndpoint(walletId.Value, user.Subject.ToString(),
                    cancellationToken);

            var issuingContract = CertificateIssuingContract.Create(
                contractNumber,
                contract.GSRN,
                matchingMeteringPoint.GridArea,
                Map(matchingMeteringPoint.Type),
                meteringPointOwner,
                startDate,
                endDate,
                walletDepositEndpoint.Endpoint.ToString(),
                walletDepositEndpoint.PublicKey.Export().ToArray(),
                Map(matchingMeteringPoint.Type, matchingMeteringPoint.Technology));

            newContracts.Add(issuingContract);

            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
                actorId: user.Subject,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: user.Name,
                organizationTin: user.Organization!.Tin,
                organizationName: user.Organization.Name,
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
        UserDescriptor user, CancellationToken cancellationToken)
    {

        var meteringPointOwner = user.Subject.ToString();
        var issuingContracts =
            await unitOfWork.CertificateIssuingContractRepo.GetAllByIds(contracts.Contracts.Select(c => c.Id).ToList(),
                cancellationToken);

        var contractsByGsrn =
            await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(issuingContracts.Select(c => c.GSRN).ToList(),
                cancellationToken);

        foreach (var updatedContract in contracts.Contracts)
        {
            DateTimeOffset? newEndDate = updatedContract.EndDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(updatedContract.EndDate.Value)
                : null;
            var existingContract = issuingContracts.Find(c => c.Id == updatedContract.Id);
            if (existingContract == null)
            {
                return new NonExistingContract();
            }

            if (existingContract?.MeteringPointOwner != meteringPointOwner)
            {
                return new MeteringPointOwnerNoMatch();
            }

            if (newEndDate != null &&
                newEndDate <= existingContract.StartDate)
            {
                return new EndDateBeforeStartDate(existingContract.StartDate, newEndDate.Value);
            }

            var overlappingContract = contractsByGsrn.FirstOrDefault(c =>
                c.Overlaps(existingContract.StartDate, newEndDate) &&
                c.Id != existingContract.Id);

            if (overlappingContract != null)
            {
                return new OverlappingContract();
            }

            existingContract.EndDate = newEndDate;

            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: user.Name,
                organizationTin: user.Organization!.Tin,
                organizationName: user.Organization.Name,
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

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner,
        CancellationToken cancellationToken)
        => unitOfWork.CertificateIssuingContractRepo.GetAllMeteringPointOwnerContracts(meteringPointOwner,
            cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner,
        CancellationToken cancellationToken)
    {
        var contract = await unitOfWork.CertificateIssuingContractRepo.GetById(id, cancellationToken);

        if (contract == null)
        {
            return null;
        }

        return contract.MeteringPointOwner.Trim() != meteringPointOwner.Trim()
            ? null
            : contract;
    }
}
