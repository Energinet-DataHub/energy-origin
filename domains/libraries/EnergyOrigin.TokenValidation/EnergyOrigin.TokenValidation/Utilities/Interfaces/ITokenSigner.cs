namespace EnergyOrigin.TokenValidation.Utilities.Interfaces;

public interface ITokenSigner
{
    string Sign(
        string subject,
        string name,
        string issuer,
        string audience,
        DateTime? issueAt = null,
        int duration = 120,
        IDictionary<string, object>? claims = null
        );
}
