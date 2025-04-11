namespace EnergyOrigin.TokenValidation.b2c;

public class AccessDescriptor(IdentityDescriptor identity)
{
    public bool IsAuthorizedToOrganization(Guid organizationId)
    {
        var isInternalClient = identity.SubjectType == SubjectType.Internal;
        var isOwnOrganization = identity.OrganizationId != Guid.Empty && identity.OrganizationId == organizationId;
        var isAuthorizedToOrganization = identity.AuthorizedOrganizationIds.Contains(organizationId);
        return isInternalClient || isOwnOrganization || isAuthorizedToOrganization;
    }

    public bool IsExternalClientAuthorized()
    {
        return identity.SubjectType == SubjectType.External && identity.OrganizationId != Guid.Empty;
    }

    public bool IsAuthorizedToOrganizations(List<Guid> organizationIds)
    {
        return organizationIds.TrueForAll(IsAuthorizedToOrganization);
    }
}
