using API.ContractService.Clients;
using DataContext.Models;
using DataContext.ValueObjects;
using Microsoft.EntityFrameworkCore;
using ProjectOrigin.WalletSystem.V1;
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
using Microsoft.IdentityModel.Tokens;
using EnergyOrigin.ActivityLog.API;

namespace API.ContractService;

internal class ContractServiceImpl : IContractService
{
    private readonly IMeteringPointsClient meteringPointsClient;
    private readonly WalletService.WalletServiceClient walletServiceClient;
    private readonly IUnitOfWork unitOfWork;

    public ContractServiceImpl(
        IMeteringPointsClient meteringPointsClient,
        WalletService.WalletServiceClient walletServiceClient,
        IUnitOfWork unitOfWork)
    {
        this.meteringPointsClient = meteringPointsClient;
        this.walletServiceClient = walletServiceClient;
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

        var contractNumber = contracts.Any()
            ? contracts.Max(c => c.ContractNumber) + 1
            : 0;

        var response = await walletServiceClient.CreateWalletDepositEndpointAsync(new CreateWalletDepositEndpointRequest(), cancellationToken: cancellationToken);
        var walletDepositEndpoint = response.WalletDepositEndpoint;

        var contract = CertificateIssuingContract.Create(
            contractNumber,
            gsrn,
            matchingMeteringPoint.GridArea,
            Map(matchingMeteringPoint.Type),
            meteringPointOwner,
            startDate,
            endDate,
            walletDepositEndpoint.Endpoint,
            walletDepositEndpoint.PublicKey.ToByteArray(),
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
        var issuingContracts = await unitOfWork.CertificateIssuingContractRepo.GetAllByIds(contracts.Select(c => c.id).ToList(), cancellationToken);

        if (issuingContracts.IsNullOrEmpty())
        {
            return new NonExistingContract();
        }

        var contractsByGsrn = await unitOfWork.CertificateIssuingContractRepo.GetByGsrn(issuingContracts.Select(c => c.GSRN).ToList(), cancellationToken);

        foreach (var issuingContract in issuingContracts)
        {
            var newContract = contracts.Find(c => c.id == issuingContract.Id);

            if (issuingContract.MeteringPointOwner != meteringPointOwner)
            {
                return new MeteringPointOwnerNoMatch();
            }

            if (newContract.newEndDate != null && newContract.newEndDate.ToDateTimeOffset() <= issuingContract.StartDate)
            {
                return new EndDateBeforeStartDate(issuingContract.StartDate, newContract.newEndDate.ToDateTimeOffset());
            }
            var overlappingContract = contractsByGsrn.FirstOrDefault(c =>  c.Overlaps(issuingContract.StartDate, newContract.newEndDate?.ToDateTimeOffset()) && c.Id != newContract.id);

            if (overlappingContract != null)
            {
                return new OverlappingContract();
            }

            issuingContract.EndDate = newContract.newEndDate?.ToDateTimeOffset() ?? null;

            await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject,
                actorType: ActivityLogEntry.ActorTypeEnum.User,
                actorName: user.Name,
                organizationTin: user.Organization!.Tin,
                organizationName: user.Organization.Name,
                otherOrganizationTin: string.Empty,
                otherOrganizationName: string.Empty,
                entityType: ActivityLogEntry.EntityTypeEnum.MeteringPoint,
                actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
                entityId: issuingContract.GSRN)
            );
        }
        unitOfWork.CertificateIssuingContractRepo.UpdateRange(issuingContracts);
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
