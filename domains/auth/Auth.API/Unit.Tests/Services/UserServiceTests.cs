using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;

namespace Tests.Services;

public class UserServiceTests
{
    private readonly IUserService userService;
    private readonly IUserRepository repository = Mock.Of<IUserRepository>();

    public UserServiceTests() => userService = new UserService(repository);

    [Fact]
    public async Task GetUserById_ShouldReturnUser_WhenUserExists()
    {
        var id = Guid.NewGuid();

        Mock.Get(repository)
            .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: new User()
            {
                Id = id,
                Name = "Amigo",
                AcceptedTermsVersion = 2,
                AllowCPRLookup = true
            });

        var result = await userService.GetUserByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result?.Id);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNull_WhenNoUserExists()
    {
        Mock.Get(repository)
            .Setup(it => it.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);

        var result = await userService.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
