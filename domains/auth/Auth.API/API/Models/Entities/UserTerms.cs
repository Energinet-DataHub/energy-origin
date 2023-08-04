using API.Values;
namespace API.Models.Entities;

public record UserTerms
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public UserTermsType Type { get; set; }
    public int AcceptedVersion { get; set; }
    public User User { get; set; } = null!;
}
