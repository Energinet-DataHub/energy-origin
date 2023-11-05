using API.ContractService.Clients;
using API.ContractService.Repositories;
using CertificateValueObjects;
using Microsoft.EntityFrameworkCore;
using ProjectOrigin.WalletSystem.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;

namespace API.ContractService;

internal class ContractServiceImpl : IContractService
{
    private readonly IMeteringPointsClient meteringPointsClient;
    private readonly ICertificateIssuingContractRepository repository;
    private readonly WalletService.WalletServiceClient walletServiceClient;

    public ContractServiceImpl(
        IMeteringPointsClient meteringPointsClient,
        WalletService.WalletServiceClient walletServiceClient,
        ICertificateIssuingContractRepository repository)
    {
        this.meteringPointsClient = meteringPointsClient;
        this.repository = repository;
        this.walletServiceClient = walletServiceClient;
    }

    public async Task<CreateContractResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, DateTimeOffset? endDate, CancellationToken cancellationToken)
    {
        var meteringPoints = await meteringPointsClient.GetMeteringPoints(meteringPointOwner, cancellationToken);
        var matchingMeteringPoint = meteringPoints?.MeteringPoints.FirstOrDefault(mp => mp.GSRN == gsrn);

        if (matchingMeteringPoint == null)
        {
            return new GsrnNotFound();
        }

        var contracts = await repository.GetByGsrn(gsrn, cancellationToken);

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
            walletDepositEndpoint.PublicKey.ToByteArray());

        try
        {
            await repository.Save(contract);

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

    public async Task<SetEndDateResult> SetEndDate(Guid id, string meteringPointOwner, DateTimeOffset? newEndDate, CancellationToken cancellationToken)
    {
        var contract = await repository.GetById(id, cancellationToken);

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
        await repository.Update(contract);

        return new SetEndDateResult.Success();
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken)
        => repository.GetAllMeteringPointOwnerContracts(meteringPointOwner, cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken)
    {
        var contract = await repository.GetById(id, cancellationToken);

        if (contract == null)
        {
            return null;
        }

        return contract.MeteringPointOwner.Trim() != meteringPointOwner.Trim()
            ? null
            : contract;
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByGSRN(string gsrn, CancellationToken cancellationToken) => repository.GetByGsrn(gsrn, cancellationToken);
}
