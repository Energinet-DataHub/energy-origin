using API.Values;

namespace API.Models.Entities;

public record CompanyTerms
{
    public Guid? Id { get; init; }
    public Guid CompanyId { get; set; }
    public CompanyTermsType Type { get; set; }
    public string AcceptedVersion { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
