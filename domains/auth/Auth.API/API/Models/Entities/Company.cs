using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(Tin))]
public record Company
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = null!;
    public string Tin { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = null!;
}
