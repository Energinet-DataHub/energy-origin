using System.Threading;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupServiceBla.Clients;

public interface IMeteringPointsClient
{
    Task<MeteringPointsResponse?> GetMeteringPoints(string owner, CancellationToken cancellationToken);
}
