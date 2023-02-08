namespace API.Utilities;

public interface ITokenIssuer
{
    Task<string> IssueAsync(string userId, DateTime? issueAt = default);
}
