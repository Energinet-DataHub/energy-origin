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

        var documents = await repository.GetByGsrn(gsrn, cancellationToken);

        var overlappingContract = documents.FirstOrDefault(d => CannotCreateContract(d, startDate, endDate));
        if (overlappingContract != null)
        {
            return new ContractAlreadyExists(overlappingContract);
        }

        try
        {
            var contract = new CertificateIssuingContract
            {
                Id = Guid.Empty,
                ContractNumber = 0,
                GSRN = gsrn,
                GridArea = matchingMeteringPoint.GridArea,
                MeteringPointType = MeteringPointType.Production,
                MeteringPointOwner = meteringPointOwner,
                StartDate = startDate,
                EndDate = endDate,
                Created = DateTimeOffset.UtcNow
            };

            await repository.Save(contract);

            return new Success(contract);
        }
        catch (DocumentAlreadyExistsException)
        {
            return new ContractAlreadyExists(null);
        }
    }

    public async Task<EndContractResult> EndContract(string meteringPointOwner, Guid contractId, DateTimeOffset? endDate, CancellationToken cancellationToken)
    {
        var contract = await repository.GetById(contractId, cancellationToken);

        if (contract == null)
        {
            return new NonExistingContract();
        }

        if (contract!.MeteringPointOwner != meteringPointOwner)
        {
            return new MeteringPointOwnerNoMatch();
        }

        if (endDate == null)
        {
            endDate = DateTimeOffset.Now;
        }

        contract.EndDate = endDate;
        await repository.Update(contract);

        return new Ended();
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken)
        => repository.GetAllMeteringPointOwnerContracts(meteringPointOwner, cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken)
    {
        var contract = await repository.GetById(id, cancellationToken);

        if (contract == null)
            return null;

        return contract.MeteringPointOwner.Trim() != meteringPointOwner.Trim()
            ? null
            : contract;
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByGSRN(string gsrn, CancellationToken cancellationToken) => repository.GetByGsrn(gsrn, cancellationToken);

    private static bool CannotCreateContract(CertificateIssuingContract document, DateTimeOffset startDate, DateTimeOffset? endDate)
    {
        return !(startDate <= document.StartDate || !(startDate < document.EndDate)) && (!(endDate > document.StartDate) || !(endDate < document.EndDate));
    }
}
