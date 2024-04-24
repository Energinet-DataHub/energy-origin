using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Authorization.Controllers;

public record AuthorizationUserRequest(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("orgId")] string OrgId,
    [property: JsonPropertyName("name")] string Name
);

public record AuthorizationClientRequest(
    [property: JsonPropertyName("sub")] string Sub
);

public record AuthorizationResponse(string Sub, string Name, string SubType, IEnumerable<string> OrgIds, string Scope);
