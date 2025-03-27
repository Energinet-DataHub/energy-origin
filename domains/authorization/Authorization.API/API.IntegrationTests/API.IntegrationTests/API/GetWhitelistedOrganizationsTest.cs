using System.Net;
using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetWhitelistedOrganizationsTest
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetWhitelistedOrganizationsTest(IntegrationTestFixture integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _api = integrationTestFixture.WebAppFactory
            .CreateApi(sub: integrationTestFixture.WebAppFactory.AdminPortalEnterpriseAppRegistrationObjectId);
    }

    [Fact]
    public async Task Given_CallToEndpoint_When_GettingWhitelistedOrganizations_Then_Return200OKWithList()
    {
        var whitelisted1 = Any.Whitelisted(Any.Tin());
        var whitelisted2 = Any.Whitelisted(Any.Tin());

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Whitelisted.AddAsync(whitelisted1, TestContext.Current.CancellationToken);
            await dbContext.Whitelisted.AddAsync(whitelisted2, TestContext.Current.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _api.GetWhitelistedOrganizations();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content
            .ReadFromJsonAsync<GetWhitelistedOrganizationsResponse>(TestContext.Current.CancellationToken);

        Assert.NotNull(content);
        Assert.IsType<GetWhitelistedOrganizationsResponse>(content);
        Assert.Contains(content.Result, item =>
            item.OrganizationId == whitelisted1.Id &&
            item.Tin == whitelisted1.Tin.Value
        );
        Assert.Contains(content.Result, item =>
            item.OrganizationId == whitelisted2.Id &&
            item.Tin == whitelisted2.Tin.Value
        );
    }
}
