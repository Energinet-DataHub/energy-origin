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

public record GrantConsentRequest(Guid IdpClientId);

public record ClientResponse(Guid IdpClientId, string Name, string RedirectUrl);

public record ClientConsentsResponseItem(Guid OrganizationId, string OrganizationName);
public record ClientConsentsResponse(IEnumerable<ClientConsentsResponseItem> Result);

public enum ClientType
{
    External = 0,
    Internal = 1
}

public record CreateClientRequest(Guid IdpClientId, string Name, ClientType ClientType, string RedicrectUrl);
public record CreateClientResponse(Guid Id, Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl);

public record UserOrganizationConsentsResponseItem(string ClientName, long ConsentDate);

public record UserOrganizationConsentsResponse(IEnumerable<UserOrganizationConsentsResponseItem> Result);

public class AcceptTermsDto
{
    public string Tin { get; set; }
    public string OrganizationName { get; set; }
    public string UserIdpId { get; set; }
    public string UserName { get; set; }
    public string TermsVersion { get; set; }
}

