using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class RetryOptions
{
    public const string Prefix = "Retry";

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }
}
