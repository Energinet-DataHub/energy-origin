using API.Models;

namespace API.UnitTests.Models;

public class RoleTests
{
    [Fact]
    public void Role_External_IsZero()
    {
        Assert.Equal(0, (int)Role.External);
    }

    [Fact]
    public void Role_Internal_IsOne()
    {
        Assert.Equal(1, (int)Role.Internal);
    }
}
