using API.Utilities;

namespace Tests.Utilities;

public class UriExtensionTests
{
    [Fact]
    public void AddQueryParameters_ShouldAddQueryParameters_WhenQueryParametersIsAdded()
    {
        var uri = new Uri("http://example.com/");
        var key = "abc";
        var value = "123";
        var expectedOutput = $"?{key}={value}";

        var result = uri.AddQueryParameters(key, value);

        Assert.Contains(expectedOutput, result.ToString());
    }

    [Theory]
    [InlineData("Cat")]
    [InlineData("Dog", "Horse", "Frog")]
    [InlineData("Fish", "Monkey", "Chicken", "Cow", "Pig")]
    public void AddQueryParameters_ShouldNotThrowException_WhenAddingOddNumberOfParameters(params string[] parameters)
    {
        var uri = new Uri("http://example.com/");

        var exception = Record.Exception(() => uri.AddQueryParameters(parameters));

        Assert.Null(exception);
    }

    [Fact]
    public void AddQueryParameters_ShouldAddCompletePairs_WhenAddingOddNumberOfParameters()
    {
        var uri = new Uri("http://example.com/");
        var key = "abc";
        var value = "123";
        var extra = "hest";
        var expectedOutput = $"?{key}={value}";

        var result = uri.AddQueryParameters(key, value, extra);

        Assert.Contains(expectedOutput, result.ToString());
        Assert.DoesNotContain(extra, result.ToString());
    }

    [Fact]
    public void AddQueryParameters_ShouldNotAddAnyParameters_WhenListOfQueryParametersIsEmpty()
    {
        var uri = new Uri("http://example.com/");

        var result = uri.AddQueryParameters();

        Assert.Equal(uri.ToString(), result.ToString());
    }

    [Fact]
    public void AddQueryParameters_ShouldNotAddAnyParameters_WhenGivenAListOfEmptyStrings()
    {
        var uri = new Uri("http://example.com/");

        var result = uri.AddQueryParameters("", "");

        Assert.Equal(uri.ToString(), result.ToString());
    }

    [Fact]
    public void AddQueryParameters_ShouldNotAddAnyParameters_WhenGivenASingleString()
    {
        var uri = new Uri("http://example.com/");

        var result = uri.AddQueryParameters("test");

        Assert.Equal(uri.ToString(), result.ToString());
    }
}
