namespace API.Utilities;

public interface ITokenIssuer
{
    Task<string> IssueAsync(string userId, string accessToken, string identityToken, DateTime? issueAt = default);
}
