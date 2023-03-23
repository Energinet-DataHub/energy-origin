using API.Values;
using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(ProviderType), nameof(ProviderKeyType), nameof(UserProviderKey))]
public record UserProvider 
{
    public Guid? Id { get; set; }
    public ProviderType ProviderType { get; set; }
    public ProviderKeyType ProviderKeyType { get; set; }
    public string UserProviderKey { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
}
