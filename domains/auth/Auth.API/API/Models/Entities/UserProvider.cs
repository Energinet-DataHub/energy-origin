using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;

namespace API.Models.Entities;

[Index(nameof(ProviderKeyType), nameof(UserProviderKey), IsUnique = true)]
public record UserProvider
{
    public Guid? Id { get; set; }
    public ProviderKeyType ProviderKeyType { get; set; }
    public string UserProviderKey { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public static List<UserProvider> ConvertDictionaryToUserProviders(Dictionary<ProviderKeyType, string> dictionary)
    {
        return dictionary.Select(x => new UserProvider()
        {
            ProviderKeyType = x.Key,
            UserProviderKey = x.Value
        })
        .ToList();
    }
}
