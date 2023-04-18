using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class DataSyncOptions
{
    public const string Prefix = "DataSync";
    //[Required]
    public Uri? Uri { get; init; }
}
