using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using API.MasterDataService;
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

        // Check if GSRN is already signed up
        var document = await repository.GetByGsrn(gsrn);

        if (document != null)
        {
            return new ContractAlreadyExists(document);
        }

        // Save
        var userObject = new CertificateIssuingContract
        {
            Id = Guid.Empty,
            GSRN = gsrn,
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = meteringPointOwner,
            SignUpStartDate = startDate,
            Created = DateTimeOffset.UtcNow
        };
        await repository.Save(userObject);

        return new Success(userObject);
    }

    public Task<IReadOnlyList<CertificateIssuingContract>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken)
        => repository.GetAllMeteringPointOwnerSignUps(meteringPointOwner, cancellationToken);

    public async Task<CertificateIssuingContract?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken)
    {
        var signUp = await repository.GetById(id, cancellationToken);

        if (signUp == null)
            return null;

        return signUp.MeteringPointOwner.Trim() != meteringPointOwner.Trim()
            ? null
            : signUp;
    }
}
