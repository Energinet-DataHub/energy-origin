using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(ProviderId))]
public record User
{
    public Guid? Id { get; init; }
    public string ProviderId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int AcceptedTermsVersion { get; set; }
    public bool AllowCPRLookup { get; set; }

    public Guid? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
}
