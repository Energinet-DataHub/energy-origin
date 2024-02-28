namespace API.Query.API.ApiModels.Requests;

public class EditContractEndDate
{
    /// <summary>
    /// End Date for generation of certificates in Unix time seconds. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}
