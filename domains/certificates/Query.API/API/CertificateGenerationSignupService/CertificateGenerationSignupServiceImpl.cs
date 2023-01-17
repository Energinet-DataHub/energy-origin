using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.CertificateGenerationSignupService.Clients;
using API.MasterDataService;
using API.Query.API.Repositories;
using static API.CertificateGenerationSignupService.CreateSignupResult;

namespace API.CertificateGenerationSignupService;

internal class CertificateGenerationSignupServiceImpl : ICertificateGenerationSignupService
{
    private readonly IMeteringPointsClient client;
    private readonly IMeteringPointSignupRepository repository;

    public CertificateGenerationSignupServiceImpl(IMeteringPointsClient client, IMeteringPointSignupRepository repository)
    {
        this.client = client;
        this.repository = repository;
    }

    public async Task<CreateSignupResult> Create(string gsrn, string meteringPointOwner, DateTimeOffset startDate, CancellationToken cancellationToken)
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
            return new SignupAlreadyExists(document);
        }

        // Save
        var userObject = new MeteringPointSignup
        {
            Id = new Guid(),
            GSRN = gsrn,
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = meteringPointOwner,
            SignupStartDate = startDate,
            Created = DateTimeOffset.UtcNow
        };
        await repository.Save(userObject);

        return new Success(userObject);
    }
}
