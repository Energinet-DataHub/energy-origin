using AuthLibrary.Utilities;

namespace API.Utilities;

public interface ITokenIssuer
{
    string Issue(UserDescriptor descriptor, bool versionBypass = false, DateTime? issueAt = default);
}
