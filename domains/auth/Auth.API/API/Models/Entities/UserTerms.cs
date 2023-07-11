namespace API.Models.Entities;

public record UserTerms
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public string TermsKey { get; set; } = null!;
    public int AcceptedVersion { get; set; }

    public User User { get; set; } = null!;
}
