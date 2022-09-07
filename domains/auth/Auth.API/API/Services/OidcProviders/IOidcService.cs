using API.Controllers.dto;
using API.Errors;
using API.Models;

namespace API.Services.OidcProviders;

public interface IOidcService
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task<OidcTokenResponse> FetchToken(string code);
    NextStep OnOidcFlowFailed(AuthState authState, OidcCallbackParams oidcCallbackParams);
    NextStep BuildFailureUrl(AuthState authState, AuthError error);
    Task Logout(string token);
}
