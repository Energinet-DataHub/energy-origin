using Microsoft.EntityFrameworkCore;

namespace API.Models;

[Index(nameof(ProviderId))]
public record User
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AcceptedTermsVersion { get; set; }
    public string? Tin { get; set; }
    public bool AllowCPRLookup { get; set; }
}
