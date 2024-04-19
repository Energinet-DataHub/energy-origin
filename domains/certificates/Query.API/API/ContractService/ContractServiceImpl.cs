using API.ContractService.Clients;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.UnitOfWork;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;
using Technology = DataContext.ValueObjects.Technology;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.ActivityLog.DataContext;
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

    public async Task<CreateContractResult> Create(string gsrn, UserDescriptor user, DateTimeOffset startDate, DateTimeOffset? endDate, CancellationToken cancellationToken)
    {
        var meteringPointOwner = user.Subject.ToString();
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(meteringPointOwner, cancellationToken);
        var matchingMeteringPoint = meteringPoints?.Result.FirstOrDefault(mp => mp.Gsrn == gsrn);

        if (matchingMeteringPoint == null)
        {
            return new GsrnNotFound();
        }

        var contracts = await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(gsrn, cancellationToken);

        var overlappingContract = contracts.FirstOrDefault(c => c.Overlaps(startDate, endDate));
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

        var contractNumber = contracts.Any()
            ? contracts.Max(c => c.ContractNumber) + 1
            : 0;

        var walletDepositEndpoint =
            await walletClient.CreateWalletEndpoint(walletId.Value, user.Subject.ToString(), cancellationToken);

        var contract = CertificateIssuingContract.Create(
            contractNumber,
            gsrn,
            matchingMeteringPoint.GridArea,
            Map(matchingMeteringPoint.Type),
            meteringPointOwner,
            startDate,
            endDate,
            walletDepositEndpoint.Endpoint.ToString(),
            walletDepositEndpoint.PublicKey.Export().ToArray(),
            Map(matchingMeteringPoint.Type, matchingMeteringPoint.Technology));

        try
        {
            await unitOfWork.CertificateIssuingContractRepo.Save(contract);
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
                entityId: gsrn)
            );
            await unitOfWork.SaveAsync();

            return new CreateContractResult.Success(contract);
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

    public async Task<SetEndDateResult> SetEndDate(List<(Guid id, UnixTimestamp? newEndDate)> contracts, UserDescriptor user, CancellationToken cancellationToken)
    {
        var meteringPointOwner = user.Subject.ToString();

        foreach (var (id, newEndDate) in contracts)
        {
            var contract = await unitOfWork.CertificateIssuingContractRepo.GetById(id, cancellationToken);

            if (contract == null)
            {
                return new NonExistingContract();
            }

            if (contract.MeteringPointOwner != meteringPointOwner)
            {
                return new MeteringPointOwnerNoMatch();
            }

            if (newEndDate != null && newEndDate.ToDateTimeOffset() <= contract.StartDate)
            {
                return new EndDateBeforeStartDate(contract.StartDate, newEndDate.ToDateTimeOffset());
            }
            var issuingContracts = await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(contract.GSRN, cancellationToken);
            var overlappingContract = issuingContracts.FirstOrDefault(c => c.Overlaps(contract.StartDate, newEndDate?.ToDateTimeOffset()) && c.Id != contract.Id);

            if (overlappingContract != null)
            {
                return new OverlappingContract();
            }

            contract.EndDate = newEndDate?.ToDateTimeOffset() ?? null;
            unitOfWork.CertificateIssuingContractRepo.Update(contract);
            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: user.Name,
                organizationTin: user.Organization!.Tin,
                organizationName: user.Organization.Name,
                otherOrganizationTin: string.Empty,
                otherOrganizationName: string.Empty,
                entityType: ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
                entityId: contract.GSRN)
            );
        }

        await unitOfWork.SaveAsync();
        return new SetEndDateResult.Success();
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken)
        => unitOfWork.CertificateIssuingContractRepo.GetAllMeteringPointOwnerContracts(meteringPointOwner, cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken)
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
