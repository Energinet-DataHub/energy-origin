using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace API.Models;

[Index(nameof(ProviderId))]
public record User
{
    public Guid Id { get; set; }
    public string ProviderId { get; set; }
    public string Name { get; set; }
    public int AcceptedTermsVersion { get; set; }
    public string? Tin { get; set; }
    public bool AllowCPRLookup { get; set; }
}
