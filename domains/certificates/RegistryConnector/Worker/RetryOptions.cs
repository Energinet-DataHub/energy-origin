using System.ComponentModel.DataAnnotations;

namespace RegistryConnector.Worker;

public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int DefaultFirstLevelRetryCount { get; set; }

    [Required]
    public int RegistryTransactionStillProcessingRetryCount { get; set; }
}
