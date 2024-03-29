using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;
using MassTransit;

namespace Unit.Tests.Services;

public class UserServiceTests
{
    private readonly IUserService userService;
    private readonly IUserRepository repository = Substitute.For<IUserRepository>();
    private readonly IPublishEndpoint publishEndpoint = Substitute.For<IPublishEndpoint>();

    public UserServiceTests() => userService = new UserService(repository, publishEndpoint);

    [Fact]
    public async Task GetUserById_ShouldReturnUser_WhenUserExists()
    {
        var id = Guid.NewGuid();

        repository.GetUserByIdAsync(Arg.Any<Guid>()).Returns(new User()
        {
            Id = id,
            Name = "Amigo",
            AllowCprLookup = true
        });

        var result = await userService.GetUserByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNull_WhenNoUserExists()
    {
        repository.GetUserByIdAsync(Arg.Any<Guid>()).Returns(null as User);

        var result = await userService.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
