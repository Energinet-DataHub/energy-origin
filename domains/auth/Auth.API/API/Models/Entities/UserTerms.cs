namespace API.Models.Entities;

public enum UserTermsType
{
    PrivacyPolicy
}

public record UserTerms
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public UserTermsType Type { get; set; }
    public int AcceptedVersion { get; set; }
    public User User { get; set; } = null!;
}
