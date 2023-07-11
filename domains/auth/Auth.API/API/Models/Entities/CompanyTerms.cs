namespace API.Models.Entities;

public record CompanyTerms
{
    public Guid? Id { get; init; }
    public Guid CompanyId { get; set; }
    public string TermsKey { get; set; } = null!;
    public int AcceptedVersion { get; set; }

    public Company Company { get; set; } = null!;
}
