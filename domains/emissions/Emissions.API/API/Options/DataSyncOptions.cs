using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class DataSyncOptions
{
    public const string Prefix = "DataSync";

    [Required]
    [RegularExpression(@"^https?:\/\/[^\s]+")]
    public Uri Endpoint { get; init; } = null!;
}
