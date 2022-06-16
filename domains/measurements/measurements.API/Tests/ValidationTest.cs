using API.Models.Request;
using API.Validation;
using Xunit;
using Xunit.Categories;

namespace Tests;

[UnitTest]
public sealed class ValidationTest
{
    [Fact]
    public void DatesNotSpecified_Validate_ErrorsReturned()
    {
        // Arrange
        var validator = new MeasurementsRequestValidator();
        var request = new MeasurementsRequest();

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
    }

    [Fact]
    public void DateFromLargerThanDateTo_Validate_ErrorsReturned()
    {
        // Arrange
        var validator = new MeasurementsRequestValidator();
        var dateFrom = 10;
        var dateTo = 1;
        var request = new MeasurementsRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }


    [Fact]
    public void DateSpecified_Validate_NoErrorsReturned()
    {
        // Arrange
        var validator = new MeasurementsRequestValidator();
        var dateFrom = 1;
        var dateTo = 10;
        var request = new MeasurementsRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}