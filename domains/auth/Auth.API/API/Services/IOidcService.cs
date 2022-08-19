using API.Models;
using API.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.Services;
public interface IOidcService
{
    QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang);
    public Task<OidcTokenResponse> FetchToken(AuthState state, string code);
}
