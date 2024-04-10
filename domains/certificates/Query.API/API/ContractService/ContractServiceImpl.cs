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

        var wallet = await unitOfWork.WalletRepo.GetWalletByOwnerSubject(user.Subject, cancellationToken);
        if (wallet == null)
        {
            var createWalletResponse = await walletClient.CreateWallet(user.Subject.ToString(), cancellationToken);

            if(createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            wallet = new Wallet(user.Subject, createWalletResponse.WalletId);
            await unitOfWork.WalletRepo.AddWallet(wallet);
            await unitOfWork.SaveAsync();
        }

        var contractNumber = contracts.Any()
            ? contracts.Max(c => c.ContractNumber) + 1
            : 0;

        var walletDepositEndpoint = await walletClient.CreateWalletEndpoint(wallet.WalletId, user.Subject.ToString(), cancellationToken);

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

    public async Task<SetEndDateResult> SetEndDate(Guid id, UserDescriptor user, DateTimeOffset? newEndDate, CancellationToken cancellationToken)
    {
        var meteringPointOwner = user.Subject.ToString();
        var contract = await unitOfWork.CertificateIssuingContractRepo.GetById(id, cancellationToken);

        if (contract == null)
        {
            return new NonExistingContract();
        }

        if (contract.MeteringPointOwner != meteringPointOwner)
        {
            return new MeteringPointOwnerNoMatch();
        }

        if (newEndDate.HasValue && newEndDate <= contract.StartDate)
        {
            return new EndDateBeforeStartDate(contract.StartDate, newEndDate.Value);
        }

        contract.EndDate = newEndDate;
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

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByGSRN(string gsrn, CancellationToken cancellationToken) => unitOfWork.CertificateIssuingContractRepo.GetByGsrn(gsrn, cancellationToken);
}
