namespace API.Models.Entities;
using Microsoft.EntityFrameworkCore;

[Index(nameof(Key), IsUnique = true)]
public record Role
{
    public Guid? Id { get; init; }
    public string Key { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Default { get; set; }
    public bool RoleAdmin { get; set; }

    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public List<User> Users { get; set; } = new List<User>();
}
