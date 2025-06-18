using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class ValidateLoginTypeIntegrationTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ValidateLoginTypeIntegrationTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _api = integrationTestFixture.WebAppFactory.CreateApi(sub: integrationTestFixture.WebAppFactory.IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task GivenLoginTypeAndOrganizationStatusMatch_WhenQueryingEndpoint_Then_Return200AndOrganizationStatus()
    {
        var org = Any.TrialOrganization();
        const string loginType = "trial";

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Organizations.AddAsync(org, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new DoesOrganizationStatusMatchLoginTypeRequest(org.Tin!.Value, loginType);
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("trial", json.GetProperty("org_status").GetString());
    }

    [Fact]
    public async Task GivenTrialLoginType_WhenOrganizationDoesNotExist_Then_Return200Ok_And_OrganizationStatusTrial()
    {
        const string loginType = "trial";
        var request = new DoesOrganizationStatusMatchLoginTypeRequest(Any.Tin().Value, loginType);
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("trial", json.GetProperty("org_status").GetString());
    }

    [Fact]
    public async Task GivenTrialLoginType_WhenOrganizationDeactivated_Then_ReturnHttp409WithB2cStatus409_And_ExpectedFailureReason()
    {
        const string loginType = "trial";
        var request = new DoesOrganizationStatusMatchLoginTypeRequest(Any.Tin().Value, loginType);
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("trial", json.GetProperty("org_status").GetString());
    }

    [Fact]
    public async Task GivenNormalLoginType_WhenOrganizationIsDeactivated_Then_Return200Ok_And_OrganizationStatusDeactivated()
    {
        var org = Any.DeactivatedOrganization();
        const string loginType = "normal";

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Organizations.AddAsync(org, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new DoesOrganizationStatusMatchLoginTypeRequest(org.Tin!.Value, loginType);
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("deactivated", json.GetProperty("org_status").GetString());
    }
}
