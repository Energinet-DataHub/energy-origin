using API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Services;
public interface ICookieService
{
    CookieOptions CreateCookieOptions(int CookieExpireDelta);

    public string IsValid();
}
