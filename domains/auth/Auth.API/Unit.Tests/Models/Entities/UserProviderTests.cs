using API.Models.Entities;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests.Services;

public class UserProviderTests
{
    [Fact]
    public void ConvertDictionaryToUserProviders_ShouldConvertDictionaryToUserProviders_WhenInvoked()
    {
        var dict = new Dictionary<ProviderKeyType, string>()
        {
            { ProviderKeyType.MitID_UUID, Guid.NewGuid().ToString() },
            { ProviderKeyType.PID, Guid.NewGuid().ToString() }
        };

        var result = UserProvider.ConvertDictionaryToUserProviders(dict);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(dict.First().Key, result.First().ProviderKeyType);
        Assert.Equal(dict.First().Value, result.First().UserProviderKey);
        Assert.Equal(dict.Last().Key, result.Last().ProviderKeyType);
        Assert.Equal(dict.Last().Value, result.Last().UserProviderKey);
    }
}
