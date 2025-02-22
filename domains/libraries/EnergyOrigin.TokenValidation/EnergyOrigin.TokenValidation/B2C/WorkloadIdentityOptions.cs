namespace EnergyOrigin.TokenValidation.b2c;

public class WorkloadIdentityOptions
{
    public const string Prefix = "WLI";

    public required string Issuer { get; set; } = null!;
}
