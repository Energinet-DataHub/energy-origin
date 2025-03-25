using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminPortal.Tests.Controllers;

public class WhitelistedOrganizationsControllerTests
{
    [Fact]
    public async Task Given_ValidTin_When_PostingToWhitelistEndpoint_Then_ReturnsRedirectToIndex()
    {
        var testTin = "12345678";
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var token = await factory.ExtractAntiforgeryTokenAsync(client, "/WhitelistedOrganizations");
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Tin", testTin),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/WhitelistedOrganizations", formContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/WhitelistedOrganizations", response.Headers.Location?.OriginalString);
    }
}
