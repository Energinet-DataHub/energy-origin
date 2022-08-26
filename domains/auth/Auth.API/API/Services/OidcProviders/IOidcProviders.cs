using API.Controllers.dto;
using API.Models;

namespace API.Services;
public interface IOidcProviders
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken);
    bool isError(OidcCallbackParams oidcCallbackParams);
    NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams);

}
