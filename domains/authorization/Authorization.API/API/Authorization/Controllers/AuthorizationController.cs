using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
public class AuthorizationController : ControllerBase
{
    /// <summary>
    /// Retreives Authorization Model.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/authorization/")]
    public async Task<ActionResult<AuthorizationResponse>> CreateContract([FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationRequest request)
    {
        var headers = string.Join("; ", Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        logger.LogWarning("Headers: {headers}, Body: {body}", headers, body);

        if (request.ClientId.Equals("529a55d0-68c7-4129-ba3c-e06d4f1038c4"))
            return new AuthorizationResponse(new[] { "123456789", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() });
        else return new AuthorizationResponse(Array.Empty<string>());
    }
}
public record AuthorizationRequest(
    [property: JsonPropertyName("client_id")] string ClientId
);

public record AuthorizationResponse(IEnumerable<string> CVRNumbers);

public static class ApiVersions
{
    public const string Version20230101 = "20230101";
}
