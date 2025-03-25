using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Response;
using AdminPortal.Tests.Setup;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;

namespace AdminPortal.Tests.Controllers;

public class WhitelistedOrganizationsControllerTests
{
    [Fact]
    public async Task Given_WhitelistedOrganizations_When_GetIndex_Then_ReturnsSuccessfulResponse()
    {
        var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient<GeneralUser>(new WebApplicationFactoryClientOptions(), 12345);

        var response = await client.GetAsync("/WhitelistedOrganizations", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_ValidTin_When_PostingToWhitelistEndpoint_Then_ReturnsRedirect()
    {
        var testTin = "12345678";
        var factory = new TestWebApplicationFactory();

        // Disable auto-redirect
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        var client = factory.CreateAuthenticatedClient<GeneralUser>(clientOptions, 12345);

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

    [Fact]
    public async Task Given_ValidTin_When_PostingToWhitelistEndpoint_Then_CanFollowRedirectToIndex()
    {
        var testTin = "12345678";
        var factory = new TestWebApplicationFactory();

        // Enable auto-redirect (default behavior)
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        };
        var client = factory.CreateAuthenticatedClient<GeneralUser>(clientOptions, 12345);

        var token = await factory.ExtractAntiforgeryTokenAsync(client, "/WhitelistedOrganizations");
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Tin", testTin),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/WhitelistedOrganizations", formContent, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("Whitelisted Organizations", content);
    }
}
