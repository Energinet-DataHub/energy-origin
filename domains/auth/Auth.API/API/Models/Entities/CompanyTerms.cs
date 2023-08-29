using API.Values;
using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(CompanyId), nameof(Type), IsUnique = true)]
public record CompanyTerms
{
    public Guid? Id { get; init; }
    public Guid CompanyId { get; set; }
    public CompanyTermsType Type { get; set; }
    public int AcceptedVersion { get; set; }
    public Company Company { get; set; } = null!;
}
