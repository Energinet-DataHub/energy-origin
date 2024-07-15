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
    [property: JsonPropertyName("org_ids")] IEnumerable<Guid> OrgIds,
    [property: JsonPropertyName("scope")] string Scope
    );

public record UserAuthorizationResponse(
    [property: JsonPropertyName("sub")] Guid Sub,
    [property: JsonPropertyName("sub_type")] string SubType,
    [property: JsonPropertyName("org_name")] string OrgName,
    [property: JsonPropertyName("org_ids")] IEnumerable<Guid> OrgIds,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("terms_accepted")] bool TermsAccepted
);

public record GrantConsentRequest(Guid IdpClientId);

public record ClientResponse(Guid IdpClientId, string Name, string RedirectUrl);

public record ClientConsentsResponseItem(Guid OrganizationId, string OrganizationName, string Tin);
public record ClientConsentsResponse(IEnumerable<ClientConsentsResponseItem> Result);

public enum ClientType
{
    External = 0,
    Internal = 1
}

public record CreateClientRequest(Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl);

public record CreateClientResponse(Guid Id, Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl);

public record UserOrganizationConsentsResponseItem(Guid IdpClientId, string ClientName, long ConsentDate);
public record UserOrganizationConsentsResponse(IEnumerable<UserOrganizationConsentsResponseItem> Result);
