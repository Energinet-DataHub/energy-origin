using System.ComponentModel.DataAnnotations;
using FluentAssertions;

namespace EnergyOrigin.ActivityLog.Unit.Tests;

public class ActivityLogOptionsTests
{
    [Fact]
    public void ActivityLogOptions_ShouldHaveDefaultValues()
    {
        var options = new ActivityLogOptions();

        options.CleanupActivityLogsOlderThanInDays.Should().Be(180);
        options.CleanupIntervalInSeconds.Should().Be(15 * 60);
    }

    [Fact]
    public void ActivityLogOptions_RequiresServiceName()
    {
        var options = new ActivityLogOptions();
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].MemberNames.Should().Contain("ServiceName");
    }

    [Fact]
    public void ActivityLogOptions_ValidatesWithServiceName()
    {
        var options = new ActivityLogOptions
        {
            ServiceName = "TestService"
        };
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}

