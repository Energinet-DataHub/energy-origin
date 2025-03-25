using System;
using System.Collections.Generic;

namespace AdminPortal.Dtos.Response;

public record FirstPartyOrganizationsResponseItem(Guid OrganizationId, string OrganizationName, string Tin);
public record GetFirstPartyOrganizationsResponse(IEnumerable<FirstPartyOrganizationsResponseItem> Result);
