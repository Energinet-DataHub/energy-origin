using EnergyOrigin.TokenValidation.Utilities;

namespace API.Utilities.Interfaces;

public interface ITokenIssuer
{
    string Issue(UserDescriptor descriptor, bool versionBypass = false, DateTime? issueAt = default);
}
