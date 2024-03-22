using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }
}
