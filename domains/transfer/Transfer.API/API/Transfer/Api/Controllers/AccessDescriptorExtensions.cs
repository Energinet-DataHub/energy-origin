using System;
using System.Collections.Generic;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.TokenValidation.b2c;

namespace API.Transfer.Api.Controllers;

internal static class AccessDescriptorExtensions
{
    internal static void AssertAuthorizedToAccessOrganization(this AccessDescriptor accessDescriptor, Guid organizationId)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            throw new ForbiddenException(organizationId);
        }
    }

    internal static void AssertAuthorizedToAccessOrganizations(this AccessDescriptor accessDescriptor, List<Guid> organizationIds)
    {
        if (!accessDescriptor.IsAuthorizedToOrganizations(organizationIds))
        {
            throw new ForbiddenException(string.Join(",", organizationIds));
        }
    }
}
