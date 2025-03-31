using System;
using System.Collections.Generic;

namespace AdminPortal.Dtos.Response;

public record GetOrganizationsResponseItem(Guid OrganizationId, string OrganizationName, string Tin);
public record GetOrganizationsResponse(IEnumerable<GetOrganizationsResponseItem> Result);
