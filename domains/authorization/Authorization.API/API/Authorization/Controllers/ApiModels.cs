using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API.Authorization.Controllers;

public record AuthorizationUserRequest(
    [property: JsonPropertyName("sub")] Guid Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("org_cvr")] string OrgCvr,
    [property: JsonPropertyName("org_name")] string OrgName
);

public record AuthorizationClientRequest(
    [property: JsonPropertyName("client_id")] Guid ClientId
);

public record AuthorizationResponse(
    [property: JsonPropertyName("sub")] Guid Sub,
    [property: JsonPropertyName("sub_type")] string SubType,
    [property: JsonPropertyName("org_name")] string OrgName,
    [property: JsonPropertyName("org_ids")]IEnumerable<Guid> OrgIds,
    [property: JsonPropertyName("scope")] string Scope
    );

public record GrantConsentRequest(Guid ClientId);

public record ClientResponse(Guid IdpClientId, string Name, string RedirectUrl);
