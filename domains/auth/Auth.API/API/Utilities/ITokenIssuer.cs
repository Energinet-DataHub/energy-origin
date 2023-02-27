namespace API.Utilities;

public interface ITokenIssuer
{
    string Issue(UserDescriptor descriptor, DateTime? issueAt = default);
}
