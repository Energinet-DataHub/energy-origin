using System;
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
}
