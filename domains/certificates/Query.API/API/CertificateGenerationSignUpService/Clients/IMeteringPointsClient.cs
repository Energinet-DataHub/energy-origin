using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignUpService.Clients;

public interface IMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);
}
