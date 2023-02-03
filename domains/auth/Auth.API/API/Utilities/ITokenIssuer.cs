using API.Options;

namespace API.Utilities;

public interface ITokenIssuer
{
    string Issue(TokenOptions options, string userId);
}
