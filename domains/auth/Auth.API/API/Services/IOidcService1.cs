using API.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.Services
{
    public interface IOidcService1
    {
        QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang);
        string EncodeBase64(this string value);
        Task<OidcTokenResponse> FetchToken(AuthState state, string code, string redirectUri);
    }
}