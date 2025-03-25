using System;
using System.Collections.Generic;

namespace AdminPortal.Dtos.Response;

public class WhitelistedOrganizationViewModel
{
    public required Guid OrganizationId { get; set; }
    public required string Tin { get; set; }
}

public record WhitelistedOrganizationsResponseItem(Guid OrganizationId, string Tin);
public record GetWhitelistedOrganizationsResponse(IEnumerable<WhitelistedOrganizationsResponseItem> Result);
