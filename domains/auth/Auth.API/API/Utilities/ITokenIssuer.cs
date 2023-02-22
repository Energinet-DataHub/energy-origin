using API.Models.Entities;

namespace API.Utilities;

public interface ITokenIssuer
{
    Task<string> IssueAsync(User user, string accessToken, string identityToken, DateTime? issueAt = default);
}
