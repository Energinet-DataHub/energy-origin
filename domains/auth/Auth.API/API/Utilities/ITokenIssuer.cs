namespace API.Utilities;

public interface ITokenIssuer
{
    Task<string> IssueAsync(UserDescriptor descriptor, DateTime? issueAt = default);
}
