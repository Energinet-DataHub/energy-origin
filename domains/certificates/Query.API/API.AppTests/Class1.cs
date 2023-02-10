using FluentAssertions;
using Xunit;

namespace API.AppTests;

public class Class1
{
    [Fact]
    public void Test1() => "true".Should().Be("true");
}
