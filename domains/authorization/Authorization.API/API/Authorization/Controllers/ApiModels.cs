using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("org_status")] string OrgStatus
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
    [property: JsonPropertyName("org_cvr")] string OrgCvr,
    [property: JsonPropertyName("login_type")] string LoginType
);

public record DoesOrganizationStatusMatchLoginTypeRequest(
    [property: JsonPropertyName("org_cvr")] string OrgCvr,
    [property: JsonPropertyName("login_type")] string LoginType
);

public record DoesOrganizationStatusMatchLoginTypeResponse(
    [property: JsonPropertyName("org_status")] string OrgStatus
);

// B2C error model - https://learn.microsoft.com/en-us/azure/active-directory-b2c/restful-technical-profile#returning-validation-error-message
public record AuthorizationErrorResponse(
    [property: JsonPropertyName("userMessage")] string UserMessage,
    [property: JsonPropertyName("version")] string Version = "1.0",
    [property: JsonPropertyName("status")] int Status = StatusCodes.Status409Conflict
);


public static class LoginFailureReasons
{
    public const string TrialOrganizationIsNotAllowedToLogInAsNormalOrganization = "a1b2c3d4-e111-4444-aaaa-aaaaaaaaaaaa - Trial Organization is not allowed to log in as a Normal Organization - Please log in as Trial Organization, or contact support, if you think this is an error";
    public const string NormalOrganizationsAreNotAllowedToLogInAsTrial = "b2c3d4e5-e222-5555-bbbb-bbbbbbbbbbbb - Normal Organization is not allowed to log in as a Trial organization - Please log in as Normal Organization, or contact support, if you think this is an error";
    public const string OrganizationIsDeactivated = "c3d4e5f6-e333-6666-cccc-cccccccccccc - Organization is deactivated - Please contact support, if you think this is an error";
    public const string UnknownLoginTypeSpecifiedInRequest = "e5f6g7h8-e444-7777-dddd-dddddddddddd - Unknown login type specified in request - Have you configured your client correctly?";
    public const string UnhandledException = "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee - Unhandled Exception";
}

public record GrantConsentToClientRequest(Guid IdpClientId);

public record GrantConsentToOrganizationRequest(Guid OrganizationId);

public record ClientResponse(Guid IdpClientId, string Name, string RedirectUrl);

public record OrganizationResponse(Guid OrganizationId, string OrganizationName, string? Tin);

public record ClientConsentsResponseItem(Guid OrganizationId, string OrganizationName, string? Tin);
public record ClientConsentsResponse(IEnumerable<ClientConsentsResponseItem> Result);

public record FirstPartyOrganizationsResponseItem(Guid OrganizationId, string OrganizationName, string Tin, string Status);
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

public record CreateClientRequest(Guid IdpClientId, string Name, ClientType ClientType, string RedirectUrl, bool IsTrial);

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
    long? StartDate,
    long? EndDate,
    string? Secret);

public record GetCredentialsResponse(IEnumerable<GetCredentialsResponseItem> Result);
public record GetCredentialsResponseItem(
    string? Hint,
    Guid KeyId,
    long? StartDate,
    long? EndDate);
