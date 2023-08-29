using EnergyOrigin.TokenValidation.Utilities;
using static API.Utilities.TokenIssuer;

namespace API.Utilities.Interfaces;

public interface ITokenIssuer
{
    string Issue(UserDescriptor descriptor, UserData data, bool versionBypass = false, DateTime? issueAt = default);
}
