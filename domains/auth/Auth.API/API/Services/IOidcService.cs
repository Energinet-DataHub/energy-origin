using API.Controllers.dto;
using API.Errors;
using API.Models;
using Jose;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json;

namespace API.Services;
public interface IOidcService
{
    QueryBuilder CreateAuthorizationRedirectUrl(string responseType, AuthState state, string lang);


}
