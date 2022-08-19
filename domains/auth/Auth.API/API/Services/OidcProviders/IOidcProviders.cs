using API.Models;

namespace API.Services;
public interface IOidcProviders
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken);
    IdTokenInfo DecodeJwt(string jwtToken);
}
