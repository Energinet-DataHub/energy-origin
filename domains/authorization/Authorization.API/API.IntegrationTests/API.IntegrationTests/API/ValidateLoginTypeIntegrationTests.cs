using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
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

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Organizations.AddAsync(org, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new DoesOrganizationStatusMatchLoginTypeRequest(org.Tin!.Value, "trial");
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("trial", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GivenLoginTypeAndOrganizationStatusNoMatch_WhenQueryingEndpoint_Then_Return403Forbidden()
    {
        var org = Any.TrialOrganization();

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Organizations.AddAsync(org, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new DoesOrganizationStatusMatchLoginTypeRequest(org.Tin!.Value, "normal");
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GivenValidLoginType_WhenOrganizationDoesNotExist_Then_Returns200OkAndOrganizationStatus()
    {
        var request = new DoesOrganizationStatusMatchLoginTypeRequest("00000000", "trial");
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("trial", json.GetProperty("status").GetString());
    }
}
