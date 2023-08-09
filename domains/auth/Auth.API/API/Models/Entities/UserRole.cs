namespace API.Models.Entities;

public record UserRole
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = null!;
    public User User { get; set; } = null!;
}
