using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(UserId), nameof(Role), IsUnique = true)]
public record UserRole
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = null!;
    public User User { get; set; } = null!;
}
