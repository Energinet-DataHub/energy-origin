using System;
using System.Collections.Generic;

namespace AdminPortal.Dtos.Response;

public class WhitelistedOrganizationViewModel
{
    public required Guid OrganizationId { get; set; }
    public required string Tin { get; set; }
    public required string CompanyName { get; set; }
    public required string Status { get; set; }
}

public record GetWhitelistedOrganizationsResponseItem(Guid OrganizationId, string Tin);
public record GetWhitelistedOrganizationsResponse(IEnumerable<GetWhitelistedOrganizationsResponseItem> Result);
