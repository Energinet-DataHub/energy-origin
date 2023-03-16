namespace API.Utilities;

public interface ITokenIssuer
{
    string Issue(ClaimsWrapper claimsWrapper, bool versionBypass = false, DateTime? issueAt = default);
}
