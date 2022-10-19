using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace apptests;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var application = new WebApplicationFactory<Program>();

        var client = application.CreateClient();

        var response = await client.GetAsync("/swagger");

        Assert.True(response.IsSuccessStatusCode);
    }
}
