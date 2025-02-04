using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class ProjectOriginOptions
{
    public const string ProjectOrigin = nameof(ProjectOrigin);
    [Required]
    public string WalletUrl { get; set; } = "";
}
