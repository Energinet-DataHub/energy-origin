using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace API.Query.API.ApiModels.Requests;

public class EditContractEndDate
{
    [Required]
    public Guid Id { get; init; }

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

public record EditContracts(List<EditContractEndDate> Contracts);
