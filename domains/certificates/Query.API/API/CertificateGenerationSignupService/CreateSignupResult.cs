using API.Query.API.Repositories;

namespace API.CertificateGenerationSignupService;

public abstract record CreateSignupResult
{
    public record Success(MeteringPointSignup MeteringPointSignup) : CreateSignupResult;

    public record GsrnNotFound : CreateSignupResult;

    public record NotProductionMeteringPoint : CreateSignupResult;

    public record SignupAlreadyExists(MeteringPointSignup Existing) : CreateSignupResult;

    private CreateSignupResult() { }
}
