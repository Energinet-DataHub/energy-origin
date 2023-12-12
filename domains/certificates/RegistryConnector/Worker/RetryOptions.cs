using System.ComponentModel.DataAnnotations;

namespace RegistryConnector.Worker;

public class RetryOptions
{
    public const string Retry = nameof(Retry);

    [Required]
    public int IssueToRegistryActivityRetryCount { get; set; }
}
