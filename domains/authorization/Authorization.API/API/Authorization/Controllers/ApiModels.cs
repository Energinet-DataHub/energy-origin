using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Authorization.Controllers;

public record AuthorizationUserRequest(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("org_cvr")] string OrgCvr,
    [property: JsonPropertyName("org_name")] string OrgName
);

public record AuthorizationClientRequest(
    [property: JsonPropertyName("client_id")] string ClientId
);

public record AuthorizationResponse(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sub_type")] string SubType,
    [property: JsonPropertyName("org_name")] string OrgName,
    [property: JsonPropertyName("org_ids")] IEnumerable<string> OrgIds,
    [property: JsonPropertyName("scope")] string Scope);
    [property: JsonPropertyName("orgId")] string OrgId,
    [property: JsonPropertyName("orgName")] string OrgName
);

public record AuthorizationClientRequest(
    [property: JsonPropertyName("sub")] string Sub
);

public record AuthorizationResponse(string Sub, string Name, string SubType, string OrgName, IEnumerable<string> OrgIds, string Scope);
