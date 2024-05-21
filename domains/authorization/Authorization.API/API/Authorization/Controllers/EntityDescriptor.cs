using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<Guid> OrgIds => _user
        .FindFirstValue("org_ids")!
        .Split(' ')
        .Select(Guid.Parse);

    public Guid Sub => Guid.Parse(_user.FindFirstValue("sub")!);
}
