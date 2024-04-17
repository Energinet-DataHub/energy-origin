using System;
using System.Collections.Generic;

namespace API.Query.API.ApiModels.Requests;

public class EditContractEndDate
{
    public Guid? Id { get; init; }

    /// <summary>
    /// End Date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}

public class MultipleEditContract
{
    public List<EditContractEndDate> Contracts { get; set; } = [];
}
