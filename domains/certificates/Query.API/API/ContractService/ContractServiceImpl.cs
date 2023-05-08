using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using DomainCertificate.Primitives;
using Marten.Exceptions;
using static API.ContractService.CreateContractResult;

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

    public async Task<CreateContractResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken)
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

        var document = await repository.GetByGsrn(gsrn, cancellationToken);

        if (document != null)
        {
            return new ContractAlreadyExists(document);
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

    public Task<CertificateIssuingContract?> GetByGSRN(string gsrn, CancellationToken cancellationToken) => repository.GetByGsrn(gsrn, cancellationToken);
}
