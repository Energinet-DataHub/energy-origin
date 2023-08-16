using API.Options;
using System.ComponentModel.DataAnnotations;

namespace Unit.Tests.Options;

public class RoleOptionsTests
{
    [Fact]
    public void Validate_ShouldReturnSuccess_WhenOptionsAreEmpty()
    {
        var options = new RoleOptions();

        var result = RoleOptions.Validate(options);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenOptionsAreGiven()
    {
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() {
                    Key = Guid.NewGuid().ToString(),
                    Name = Guid.NewGuid().ToString()
                }
            }
        };

        var result = RoleOptions.Validate(options);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Validate_ShouldFail_WhenOptionsContainsDuplicateKeys()
    {
        var key = Guid.NewGuid().ToString();
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() {
                    Key = key,
                    Name = Guid.NewGuid().ToString()
                },
                new() {
                    Key = key,
                    Name = Guid.NewGuid().ToString()
                }
            }
        };

        var result = RoleOptions.Validate(options);

        Assert.NotNull(result);
        Assert.IsType<ValidationResult>(result);
    }


    [Fact]
    public void Validate_ShouldFail_WhenOptionsInheritsUnknownRoles()
    {
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() {
                    Key = Guid.NewGuid().ToString(),
                    Name = Guid.NewGuid().ToString(),
                    Inherits = new() { Guid.NewGuid().ToString() }
                }
            }
        };

        var result = RoleOptions.Validate(options);

        Assert.NotNull(result);
        Assert.IsType<ValidationResult>(result);
    }
}
