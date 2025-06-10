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

    [Theory]
    [InlineData("normal", "trial", "a1b2c3d4-e111-4444-aaaa-aaaaaaaaaaaa - Trial Organization is not allowed to log in as a Normal Organization - Please log in as Trial Organization, or contact support, if you think this is an error")]
    [InlineData("trial", "normal", "b2c3d4e5-e222-5555-bbbb-bbbbbbbbbbbb - Normal Organization is not allowed to log in as a Trial organization - Please log in as Normal Organization, or contact support, if you think this is an error")]
    [InlineData("trial", "deactivated", "c3d4e5f6-e333-6666-cccc-cccccccccccc - Organization is deactivated - Please contact support, if you think this is an error")]
    [InlineData("normal", "deactivated", "c3d4e5f6-e333-6666-cccc-cccccccccccc - Organization is deactivated - Please contact support, if you think this is an error")]
    [InlineData("vip", "normal", "d4e5f6g7-e999-8888-eeee-eeeeeeeeeeee - Unhandled Exception")]
    public async Task GivenLoginTypeAndOrgStatusMismatch_WhenQueryingEndpoint_Then_ReturnHttp403WithB2cStatus409_And_ExpectedFailureReason(string loginTypeRequestParameter, string orgStatusQueryHandlerResult, string expectedReason)
    {
        var tin = orgStatusQueryHandlerResult switch
        {
            "trial" => "87654321",
            "normal" => "12345678",
            "deactivated" => "69696969",
            _ => throw new ArgumentException("Unsupported org status")
        };

        var org = orgStatusQueryHandlerResult switch
        {
            "trial" => Any.TrialOrganization(tin: Tin.Create(tin)),
            "normal" => Any.Organization(tin: Tin.Create(tin)),
            "deactivated" => Any.DeactivatedOrganization(tin: Tin.Create(tin)),
            _ => throw new ArgumentException("Unsupported org status")
        };

        await using (var dbContext = new ApplicationDbContext(_options))
        {
            await dbContext.Organizations.AddAsync(org, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new DoesOrganizationStatusMatchLoginTypeRequest(org.Tin!.Value, loginTypeRequestParameter);
        var response = await _api.GetDoesLoginTypeMatch(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(expectedReason, json.GetProperty("userMessage").GetString());
        Assert.Equal("1.0", json.GetProperty("version").GetString());
        Assert.Equal(409, json.GetProperty("status").GetInt32());
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
