using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.CertificateGenerationSignupService.Repositories;

public interface IMeteringPointSignupRepository
{
    Task Save(MeteringPointSignup meteringPointSignup);
    Task<MeteringPointSignup?> GetByGsrn(string gsrn);
    Task<IReadOnlyList<MeteringPointSignup>> GetAllSignUps(string owner);
    Task<IEnumerable<MeteringPointSignup>> GetAll(); //To be used by DataSync
    Task<IEnumerable<MeteringPointSignup>> GetByMeteringPointOwner(string meteringPointOwner);
}
