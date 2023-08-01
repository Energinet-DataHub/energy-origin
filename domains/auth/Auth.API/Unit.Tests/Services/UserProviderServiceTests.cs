using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace Unit.Tests.Services;

public class UserProviderServiceTests
{
    private readonly IUserProviderService userProviderService;
    private readonly IUserProviderRepository repository = Mock.Of<IUserProviderRepository>();

    public UserProviderServiceTests() => userProviderService = new UserProviderService(repository);

    [Fact]
    public async Task FindUserProviderMatchAsync_ShouldReturnUserProvider_WhenUserProviderMatchIsFound()
    {
        var userProviders = new List<UserProvider>()
        {
            new UserProvider()
        };

        Mock.Get(repository)
            .Setup(x => x.FindUserProviderMatchAsync(It.IsAny<List<UserProvider>>()))
            .ReturnsAsync(value: userProviders.First());

        var result = await userProviderService.FindUserProviderMatchAsync(userProviders);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindUserProviderMatchAsync_ShouldReturnNull_WhenUserProviderMatchIsNotFound()
    {
        Mock.Get(repository)
            .Setup(x => x.FindUserProviderMatchAsync(It.IsAny<List<UserProvider>>()))
            .ReturnsAsync(value: null);

        var result = await userProviderService.FindUserProviderMatchAsync(new List<UserProvider>()
        {
            new UserProvider()
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task FindUserProviderMatchAsync_ShouldReturnNull_WhenEmptyListIsProvided()
    {
        var result = await userProviderService.FindUserProviderMatchAsync(new List<UserProvider>());

        Assert.Null(result);
    }

    [Fact]
    public void GetNonMatchingUserProviders_ShouldReturnUserProviderListWithNonMatches_WhenInvokedWithNonMatchingLists()
    {
        var expectedProviderKeyType = ProviderKeyType.Pid;
        var expectedUserProviderKey = Guid.NewGuid().ToString();

        var newUserProviders = new List<UserProvider>()
        {
            new UserProvider()
            {
                ProviderKeyType = ProviderKeyType.MitIdUuid,
                UserProviderKey = Guid.NewGuid().ToString()
            },
            new UserProvider()
            {
                ProviderKeyType = expectedProviderKeyType,
                UserProviderKey = expectedUserProviderKey
            }
        };
        var oldUserProviders = new List<UserProvider>()
        {
            new UserProvider()
            {
                ProviderKeyType = ProviderKeyType.Rid,
                UserProviderKey = Guid.NewGuid().ToString()
            },
            newUserProviders.First()
        };

        var result = userProviderService.GetNonMatchingUserProviders(newUserProviders, oldUserProviders);

        Assert.Single(result);
        Assert.Equal(expectedProviderKeyType, result.Single().ProviderKeyType);
        Assert.Equal(expectedUserProviderKey, result.Single().UserProviderKey);
    }
}
