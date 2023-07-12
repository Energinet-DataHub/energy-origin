using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using CertificateValueObjects;
using Marten.Exceptions;
using static API.ContractService.CreateContractResult;
using static API.ContractService.EndContractResult;

namespace API.ContractService;

internal class ContractServiceImpl : IContractService
{
    private readonly IMeteringPointsClient client;
    private readonly ICertificateIssuingContractRepository repository;

    public ContractServiceImpl(IMeteringPointsClient client, ICertificateIssuingContractRepository repository)
    {
        this.client = client;
        this.repository = repository;
    }

    public async Task<CreateContractResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var meteringPoints = await client.GetMeteringPoints(meteringPointOwner, cancellationToken);
        var matchingMeteringPoint = meteringPoints?.MeteringPoints.FirstOrDefault(mp => mp.GSRN == gsrn);

        if (matchingMeteringPoint == null)
        {
            return new GsrnNotFound();
        }

        if (matchingMeteringPoint.Type != MeterType.Production)
        {
            return new NotProductionMeteringPoint();
        }

        var contracts = await repository.GetByGsrn(gsrn, cancellationToken);

        var overlappingContract = contracts.FirstOrDefault(d => IsOverlapping(d, startDate, endDate));
        if (overlappingContract != null)
        {
            return new ContractAlreadyExists(overlappingContract);
        }

        var contractNumber = contracts.Any()
            ? contracts.Max(c => c.ContractNumber) + 1
            : 0;

        try
        {
            var contract = new CertificateIssuingContract
            {
                Id = Guid.Empty,
                ContractNumber = contractNumber,
                GSRN = gsrn,
                GridArea = matchingMeteringPoint.GridArea,
                MeteringPointType = MeteringPointType.Production,
                MeteringPointOwner = meteringPointOwner,
                StartDate = startDate,
                EndDate = endDate,
                Created = DateTimeOffset.UtcNow
            };

            await repository.Save(contract);

            return new CreateContractResult.Success(contract);
        }
        catch (DocumentAlreadyExistsException)
        {
            return new ContractAlreadyExists(null);
        }
    }

    public async Task<EndContractResult> SetEndDate(Guid id, string meteringPointOwner, DateTimeOffset? newEndDate, CancellationToken cancellationToken)
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

        if (newEndDate.HasValue && contract.StartDate > newEndDate)
        {
            return new EndDateBeforeStartDate(contract.StartDate, newEndDate.Value);
        }

        contract.EndDate = newEndDate;
        await repository.Update(contract);

        return new EndContractResult.Success();
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

    private static bool IsOverlapping(CertificateIssuingContract contract, DateTimeOffset startDate, DateTimeOffset? endDate)
    {
        return !(startDate >= contract.EndDate || endDate <= contract.StartDate);

        //if (startDate >= contract.EndDate)
        //    return false;

        //if (endDate <= contract.StartDate)
        //    return false;

        //return true;


        //var check1 = startDate <= document.StartDate;
        //var check2 = !(startDate < document.EndDate);
        //var check3 = !(endDate > document.StartDate);
        //var check4 = !(endDate < document.EndDate);

        //return !(check1 || check2) && (check3 || check4);
    }
}
