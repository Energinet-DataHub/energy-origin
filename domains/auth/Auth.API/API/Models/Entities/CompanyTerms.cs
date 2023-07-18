namespace API.Models.Entities;

public enum CompanyTermsType
{
    TermsOfService
}
public record CompanyTerms
{
    public Guid? Id { get; init; }
    public Guid CompanyId { get; set; }
    public CompanyTermsType Type { get; set; }
    public int AcceptedVersion { get; set; }
    public Company Company { get; set; } = null!;
}
