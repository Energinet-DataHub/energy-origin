using API.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.Services;
public interface ITokenService
{
    string EncodeJwtToken(AuthState state);

    QueryBuilder CreateAuthorizationRedirectUrl(string responseType, string feUrl, AuthState state, string lang);
}
