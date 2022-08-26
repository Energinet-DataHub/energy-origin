using API.Controllers.dto;
using API.Errors;
using API.Models;
using System.Text.Json;

namespace API.Services;
public interface IOidcProviders
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken);
    public Task<JsonElement> FetchToken(AuthState state, string code, string redirectUri);
    bool isError(OidcCallbackParams oidcCallbackParams);
    NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams);
    NextStep BuildFailureUrl(AuthState authState, AuthError error);
}
