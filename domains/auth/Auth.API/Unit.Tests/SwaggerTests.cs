using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

[UsesVerify]
public class SwaggerTests
{
    [Fact]
    public async Task GetSwaggerDoc_AppStarted_NoChangesAccordingToSnapshot()
    {
        var client = new WebApplicationFactory<Program>().CreateClient();

        var swaggerDocResponse = await client.GetAsync("swagger/v1/swagger.json");

        var json = await swaggerDocResponse.Content.ReadAsStringAsync();
        await Verify(json);
    }
}
