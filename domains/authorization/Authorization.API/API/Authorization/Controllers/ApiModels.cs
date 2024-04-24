using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Authorization.Controllers;

public record AuthorizationUserRequest(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("orgId")] string OrgId,
    [property: JsonPropertyName("orgName")] string OrgName
);

public record AuthorizationClientRequest(
    [property: JsonPropertyName("sub")] string Sub
);

public record AuthorizationResponse(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
