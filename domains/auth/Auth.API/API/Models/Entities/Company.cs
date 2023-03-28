using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(Tin), IsUnique = true)]
public record Company
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = null!;
    public string Tin { get; set; } = null!;

    public virtual List<User> Users { get; set; } = new List<User>();
}
