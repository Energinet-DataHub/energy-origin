using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.MasterDataService;

namespace API.Query.API.Repositories;

public interface IMeteringPointSignupRepository
{
    Task Save(MeteringPointSignup meteringPointSignup);
    Task<MeteringPointSignup?> GetByGsrn(string gsrn);
    Task<IEnumerable<MeteringPointSignup>> GetAll(); //To be used by DataSync
    Task<IEnumerable<MeteringPointSignup>> GetByMeteringPointOwner(string meteringPointOwner);
}

public class MeteringPointSignup
{
    public Guid Id { get; set; }
    public string GSRN { get; set; }
    public MeteringPointType MeteringPointType { get; set; }
    public string MeteringPointOwner { get; set; }
    public DateTimeOffset SignupStartDate { get; set; }
    public DateTimeOffset Created { get; set; }
}
