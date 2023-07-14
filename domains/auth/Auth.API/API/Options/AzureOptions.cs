using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class AzureOptions
{
    public const string Prefix = "Azure";

    [Required]
    public string StorageAccountName { get; init; } = null!;
    [Required]
    public string StorageAccountKey { get; init; } = null!;
}
