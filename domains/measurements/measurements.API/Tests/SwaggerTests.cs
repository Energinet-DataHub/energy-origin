using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using VerifyXunit;
using Xunit;

namespace Tests;

[UsesVerify]
public class SwaggerTests
{
    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        var client = new WebApplicationFactory<Program>().CreateClient();

        var swaggerDocResponse = await client.GetAsync("api-docs/measurements/v1/swagger.json");

        var json = await swaggerDocResponse.Content.ReadAsStringAsync();
        await Verifier.Verify(json);
    }
}
