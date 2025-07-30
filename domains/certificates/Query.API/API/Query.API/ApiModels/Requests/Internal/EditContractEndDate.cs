using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

public record EditContracts(
        List<EditContractEndDate> Contracts,
        [Required]
        Guid MeteringPointOwnerId,
        [Required]
        string OrganizationTin,
        [Required]
        string OrganizationName);
