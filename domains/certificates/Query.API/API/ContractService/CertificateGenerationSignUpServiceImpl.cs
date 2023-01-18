using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using API.MasterDataService;
using static API.ContractService.CreateSignUpResult;

namespace API.ContractService;

internal class CertificateGenerationSignUpServiceImpl : ICertificateGenerationSignUpService
{
    private readonly IMeteringPointsClient client;
    private readonly ICertificateGenerationSignUpRepository repository;

    public CertificateGenerationSignUpServiceImpl(IMeteringPointsClient client, ICertificateGenerationSignUpRepository repository)
    {
        this.client = client;
        this.repository = repository;
    }

    public async Task<CreateSignUpResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken)
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
            return new SignUpAlreadyExists(document);
        }

        // Save
        var userObject = new CertificateGenerationSignUp
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

    public Task<IReadOnlyList<CertificateGenerationSignUp>> GetByOwner(string meteringPointOwner, CancellationToken cancellationToken)
        => repository.GetAllMeteringPointOwnerSignUps(meteringPointOwner, cancellationToken);

    public async Task<CertificateGenerationSignUp?> GetById(Guid id, string meteringPointOwner, CancellationToken cancellationToken)
    {
        var signUp = await repository.GetById(id, cancellationToken);

        if (signUp == null)
            return null;

        return signUp.MeteringPointOwner.Trim() != meteringPointOwner.Trim()
            ? null
            : signUp;
    }
}
