using System;
using System.Collections.Generic;

namespace API.Query.API.ApiModels.Requests.Internal;

public class EditContractEndDate
{
    public required Guid Id { get; init; }

    /// <summary>
    /// End Date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}

public class EditContractEndDate20230101
{
    /// <summary>
    /// End Date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}

// TODO: CABOL - Are we validating all input is there?
public record EditContracts(List<EditContractEndDate> Contracts, Guid MeteringPointOwnerId, string OrganizationTin, string OrganizationName);
