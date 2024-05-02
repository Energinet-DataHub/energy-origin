using Oidc.Mock.Models;
using Xunit;

namespace Tests;

public class UserTest
{
    [Fact]
    public void CanLookupSubject()
    {
        var id = Guid.NewGuid().ToString();
        var user = new User(
            Name: "Seth Subject",
            Description: "Fitive user",
            ImageUrl: "https://example.com/image.png",
            AccessToken: [],
            IdToken: new Dictionary<string, object> { ["sub"] = id },
            UserinfoToken: [],
            isSelectable: false);

        Assert.NotNull(user.Subject);
        Assert.Equal(id, user.Subject!);
    }
}
