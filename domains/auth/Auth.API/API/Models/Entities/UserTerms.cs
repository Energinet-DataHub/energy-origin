namespace API.Models.Entities;

public enum TermsType
{
    PrivacyPolicy,
    TermsOfService
}

public record UserTerms
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public TermsType Type { get; set; }
    public int AcceptedVersion { get; set; }

    public User User { get; set; } = null!;
}
