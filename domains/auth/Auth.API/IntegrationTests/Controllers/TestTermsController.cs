using System.Text.Json;
using API.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Controllers;

[UnitTest]
public class TestTermsController
{
    private readonly WebApplicationFactory<Program> factory;

    private string resources = Directory
        .GetParent(Directory.GetCurrentDirectory())?
        .Parent?
        .Parent?
        .FullName + "\\resources";

    public TestTermsController() => factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment("IntegrationTests")
            .UseSetting("TERMS_MARKDOWN_FOLDER", resources)
        );

    [Fact]
    public async Task Get_Endpoint_ReturnsSuccess()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/terms");

        Assert.True(response.IsSuccessStatusCode);
    }
}
