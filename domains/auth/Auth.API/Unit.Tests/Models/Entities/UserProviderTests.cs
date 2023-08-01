using API.Models.Entities;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests.Models.Entities;

public class UserProviderTests
{
    [Fact]
    public void ConvertDictionaryToUserProviders_ShouldConvertDictionaryToUserProviders_WhenInvoked()
    {
        var dict = new Dictionary<ProviderKeyType, string>()
        {
            { ProviderKeyType.MitIdUuid, Guid.NewGuid().ToString() },
            { ProviderKeyType.Pid, Guid.NewGuid().ToString() }
        };

        var result = UserProvider.ConvertDictionaryToUserProviders(dict);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(dict.First().Key, result.First().ProviderKeyType);
        Assert.Equal(dict.First().Value, result.First().UserProviderKey);
        Assert.Equal(dict.Last().Key, result.Last().ProviderKeyType);
        Assert.Equal(dict.Last().Value, result.Last().UserProviderKey);
    }
}
