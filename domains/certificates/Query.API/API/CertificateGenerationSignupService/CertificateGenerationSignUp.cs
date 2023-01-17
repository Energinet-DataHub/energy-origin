using System;
using API.MasterDataService;

namespace API.CertificateGenerationSignupService;

public class CertificateGenerationSignUp
{
    public Guid Id { get; set; }
    public string GSRN { get; set; } = "";
    public MeteringPointType MeteringPointType { get; set; }
    public string MeteringPointOwner { get; set; } = "";
    public DateTimeOffset SignUpStartDate { get; set; }
    public DateTimeOffset Created { get; set; }
}
