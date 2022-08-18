using API.Models;
using Microsoft.AspNetCore.Http.Extensions;

namespace API.Services;
public interface ICookieService
{
    CookieOptions CreateCookieOptions(int CookieExpireDelta);
}
