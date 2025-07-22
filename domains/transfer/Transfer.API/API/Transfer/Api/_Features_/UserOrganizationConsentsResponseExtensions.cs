using System;
using System.Linq;
using API.Transfer.Api.Clients;
using EnergyOrigin.Domain.ValueObjects;

namespace API.Transfer.Api._Features_;

public static class UserOrganizationConsentsResponseExtensions
{
    public static (OrganizationId OrganizationId, Tin OrganizationTin, OrganizationName OrganizationName) GetCurrentOrganizationBehalfOf(this UserOrganizationConsentsResponse userOrganizationConsentsResponse, Guid organizationId)
    {
        var consent = userOrganizationConsentsResponse!.Result.First(c => c.GiverOrganizationId == organizationId);

        return (OrganizationId.Create(consent.GiverOrganizationId), Tin.Create(consent.GiverOrganizationTin), OrganizationName.Create(consent.GiverOrganizationName));
    }
}
