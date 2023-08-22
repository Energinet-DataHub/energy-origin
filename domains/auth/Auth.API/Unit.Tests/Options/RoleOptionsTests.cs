using System.ComponentModel.DataAnnotations;
using API.Options;

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

        Assert.NotEqual(ValidationResult.Success, result);
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

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.IsType<ValidationResult>(result);
    }

    [Fact]
    public void Validate_ShouldFail_WhenRoleInheritsItself()
    {
        var key = Guid.NewGuid().ToString();
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() {
                    Key = key,
                    Name = Guid.NewGuid().ToString(),
                    Inherits = new() { key }
                }
            }
        };

        var result = RoleOptions.Validate(options);

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.IsType<ValidationResult>(result);
    }

    [Fact(Timeout = 1000)]
    public async Task Validate_ShouldFail_WhenRoleInheritanceHasCircularReference()
    {
        var first = Guid.NewGuid().ToString();
        var second = Guid.NewGuid().ToString();
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() {
                    Key = first,
                    Name = Guid.NewGuid().ToString(),
                    Inherits = new() { second }
                },
                new() {
                    Key = second,
                    Name = Guid.NewGuid().ToString(),
                    Inherits = new() { first }
                }
            }
        };

        var result = await Task.Run(() => RoleOptions.Validate(options)); // NOTE: *Must be* wrapped in `Task.run(...)` to ensure the timeout value is enforced!

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.IsType<ValidationResult>(result);
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenALotOfOptionsAreGiven()
    {
        var matched = "matched";
        var intermediate = "intermediate";
        var halfway = "halfway";
        var transitional = "transitional";
        var required = "required";
        var options = new RoleOptions()
        {
            RoleConfigurations = new() {
                new() { Key = matched, Name = matched, Inherits = new() { intermediate } },
                new() { Key = intermediate, Name = intermediate, Inherits = new() { halfway } },
                new() { Key = halfway, Name = halfway, Inherits = new() { transitional } },
                new() { Key = transitional, Name = transitional, Inherits = new() { required } },
                new() { Key = required, Name = required }
            }
        };

        var result = RoleOptions.Validate(options);

        Assert.Equal(ValidationResult.Success, result);
    }
}
