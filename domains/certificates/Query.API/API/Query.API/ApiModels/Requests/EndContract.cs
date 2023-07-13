namespace API.Query.API.ApiModels.Requests;

// TODO: Do we want to use this name?
public class EndContract
{
    /// <summary>
    /// End Date for generation of certificates in Unix time. Set to null for no end date
    /// </summary>
    public long? EndDate { get; set; }
}
