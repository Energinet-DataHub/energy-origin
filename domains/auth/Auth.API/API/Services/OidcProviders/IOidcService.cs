using System.Text.Json;
using API.Controllers.dto;
using API.Errors;
using API.Models;

namespace API.Services.OidcProviders;

public interface IOidcService
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<T> FetchUserInfo<T>(OidcTokenResponse oidcToken);
    Task<OidcTokenResponse> FetchToken(AuthState state, string code);
    bool isError(OidcCallbackParams oidcCallbackParams);
    NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams);
    NextStep BuildFailureUrl(AuthState authState, AuthError error);
    Task Logout(string token);
}
