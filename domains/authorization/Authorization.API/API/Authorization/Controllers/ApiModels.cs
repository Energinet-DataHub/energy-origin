using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using API.Authorization._Features_.Internal;
using Microsoft.AspNetCore.Http;

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
    [property: JsonPropertyName("org_id")] Guid OrgId,
    [property: JsonPropertyName("org_ids")] IEnumerable<Guid> OrgIds,
    [property: JsonPropertyName("scope")] string Scope
);

public record UserAuthorizationResponse(
    [property: JsonPropertyName("sub")] Guid Sub,
    [property: JsonPropertyName("sub_type")] string SubType,
    [property: JsonPropertyName("org_name")] string OrgName,
    [property: JsonPropertyName("org_id")] Guid OrgId,
    [property: JsonPropertyName("org_ids")] IEnumerable<Guid> OrgIds,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("terms_accepted")] bool TermsAccepted
);

public record WhitelistedOrganizationRequest(
    [property: JsonPropertyName("org_cvr")] string OrgCvr
);

// B2C error model - https://learn.microsoft.com/en-us/azure/active-directory-b2c/restful-technical-profile#returning-validation-error-message
public record AuthorizationErrorResponse(
    [property: JsonPropertyName("userMessage")] string UserMessage,
    [property: JsonPropertyName("version")] string Version = "1.0",
    [property: JsonPropertyName("status")] int Status = StatusCodes.Status409Conflict
);

public record GrantConsentToClientRequest(Guid IdpClientId);

public record GrantConsentToOrganizationRequest(Guid OrganizationId);

public record ClientResponse(Guid IdpClientId, string Name, string RedirectUrl);

public record OrganizationResponse(Guid OrganizationId, string OrganizationName, string? Tin);

public record ClientConsentsResponseItem(Guid OrganizationId, string OrganizationName, string? Tin);
public record ClientConsentsResponse(IEnumerable<ClientConsentsResponseItem> Result);

public record FirstPartyOrganizationsResponseItem(Guid OrganizationId, string OrganizationName, string Tin);
public record FirstPartyOrganizationsResponse(IEnumerable<FirstPartyOrganizationsResponseItem> Result);

public record GetWhitelistedOrganizationsResponseItem(Guid OrganizationId, string Tin);
public record GetWhitelistedOrganizationsResponse(IEnumerable<GetWhitelistedOrganizationsResponseItem> Result);

public record AddOrganizationToWhitelistRequest(string Tin);
public record AddOrganizationToWhitelistResponse(string Tin);

public enum ClientType
{
    External = 0,
    Internal = 1
}

public record CreateClientRequest(Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl);

public record CreateClientResponse(Guid Id, Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl);

public record UserOrganizationConsentsResponseItem(Guid ConsentId, Guid GiverOrganizationId, string GiverOrganizationTin, string GiverOrganizationName, Guid ReceiverOrganizationId, string ReceiverOrganizationTin, string ReceiverOrganizationName, long ConsentDate);
public record UserOrganizationConsentsResponse(IEnumerable<UserOrganizationConsentsResponseItem> Result);



public record UserOrganizationConsentsReceivedResponseItem(Guid ConsentId, Guid OrganizationId, string OrganizationName, string Tin, long ConsentDate);

public record UserOrganizationConsentsReceivedResponse(IEnumerable<UserOrganizationConsentsReceivedResponseItem> Result);

public record AcceptServiceProviderTermsResponse(string Message);

public record GetServiceProviderTermsResponse(bool TermsAccepted);



public record UserinfoRequest(string MitIDBearerToken);

public record MitIdUserinfoResponse(
    [property: JsonPropertyName("idp")] string Idp,
    [property: JsonPropertyName("idp_identity_id")] string IdpIdentityId,
    [property: JsonPropertyName("loa")] string Loa,
    [property: JsonPropertyName("ial")] string Ial,
    [property: JsonPropertyName("nemlogin.persistent_professional_id")] string NemloginPersistentProfessionalId,
    [property: JsonPropertyName("nemlogin.name")] string NemloginName,
    [property: JsonPropertyName("nemlogin.ial")] string NemloginIal,
    [property: JsonPropertyName("nemlogin.org_name")] string NemloginOrgName,
    [property: JsonPropertyName("nemlogin.cvr")] string NemloginCvr,
    [property: JsonPropertyName("nemlogin.nemid.rid")] string NemloginNemidRid,
    [property: JsonPropertyName("nemlogin.given_name")] string NemloginGivenName,
    [property: JsonPropertyName("nemlogin.family_name")] string NemloginFamilyName,
    [property: JsonPropertyName("nemlogin.email")] string NemloginEmail,
    [property: JsonPropertyName("nemlogin.age")] string NemloginAge,
    [property: JsonPropertyName("nemlogin.cpr_uuid")] string NemloginCprUuid,
    [property: JsonPropertyName("nemlogin.date_of_birth")] string NemloginDateOfBirth,
    [property: JsonPropertyName("mitid.has_cpr")] string MitidHasCpr,
    [property: JsonPropertyName("sub")] string Sub
);

public record UserinfoResponse(
    [property: JsonPropertyName("mitid_sub")] string Sub,
    [property: JsonPropertyName("mitid_name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("org_cvr")] string OrgCvr,
    [property: JsonPropertyName("org_name")] string OrgName
);

public record CreateCredentialResponse(
    string? Hint,
    Guid KeyId,
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime,
    string? Secret);

public record GetCredentialsResponse(IEnumerable<GetCredentialsResponseItem> Result);
public record GetCredentialsResponseItem(
    string? Hint,
    Guid KeyId,
    DateTimeOffset? StartDateTime,
    DateTimeOffset? EndDateTime);
