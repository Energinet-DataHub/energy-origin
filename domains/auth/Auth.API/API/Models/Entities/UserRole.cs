namespace API.Models.Entities;

public record UserRole
{
    public Guid? Id { get; init; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
