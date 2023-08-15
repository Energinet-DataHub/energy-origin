using API.Values;
using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(UserId), nameof(Type), IsUnique = true)]
public record UserTerms
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public UserTermsType Type { get; set; }
    public int AcceptedVersion { get; set; }
    public User User { get; set; } = null!;
}
