using API.Controllers.dto;
using API.Errors;
using API.Models;
using System.Text.Json;

namespace API.Services.OidcProviders;

public interface IOidcService
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken);
    public Task<JsonElement> FetchToken(AuthState state, string code, string redirectUri);
    bool isError(OidcCallbackParams oidcCallbackParams);
    NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams);
    NextStep BuildFailureUrl(AuthState authState, AuthError error);
    Task Logout(string token);
}
