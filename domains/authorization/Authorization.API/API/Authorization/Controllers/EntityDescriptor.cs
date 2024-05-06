using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace API.Authorization.Controllers;


public class EntityDescriptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ClaimsPrincipal _user;

    public EntityDescriptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _user = _httpContextAccessor.HttpContext!.User;
    }

    public Guid Sub
    {
        get
        {
            return Guid.Parse(_user.FindFirstValue("sub")!);
        }
    }
}
