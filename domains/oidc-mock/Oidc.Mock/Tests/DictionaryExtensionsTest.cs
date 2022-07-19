using Oidc.Mock.Extensions;
using Xunit;

namespace Tests;

public class DictionaryExtensionsTest
{
    [Fact]
    public void CanAddDictionaries()
    {
        var dict1 = new Dictionary<string, object> { { "a", 1 } };
        var dict2 = new Dictionary<string, object> { { "b", 2 } };
        var actual = dict1.Plus(dict2);

        Assert.Equal(new Dictionary<string, object> { { "a", 1 }, { "b", 2 } }, actual);
    }

    [Fact]
    public void CanAddEmptyDictionary()
    {
        var dict1 = new Dictionary<string, object> { { "a", 1 } };
        var dict2 = new Dictionary<string, object>();
        var actual = dict1.Plus(dict2);

        Assert.Equal(new Dictionary<string, object> { { "a", 1 } }, actual);
    }

    [Fact]
    public void CanAddNull()
    {
        var dict1 = new Dictionary<string, object> { { "a", 1 } };
        var actual = dict1.Plus(null!);

        Assert.Equal(new Dictionary<string, object> { { "a", 1 } }, actual);
    }

    [Fact]
    public void ThrowsForSameKey()
    {
        var dict1 = new Dictionary<string, object> { { "a", 1 } };
        var dict2 = new Dictionary<string, object> { { "a", 2 } };

        Assert.Throws<ArgumentException>(() => dict1.Plus(dict2));
    }
}
