using Microsoft.AspNetCore.Http;

namespace API.Transfer.Api.Clients;

public class WebContextBearerTokenService(IHttpContextAccessor HttpContextAccessor) : IBearerTokenService
{
    public string GetBearerToken()
    {
        return HttpContextAccessor.HttpContext!.Request.Headers["Authorization"]!;
    }
}
