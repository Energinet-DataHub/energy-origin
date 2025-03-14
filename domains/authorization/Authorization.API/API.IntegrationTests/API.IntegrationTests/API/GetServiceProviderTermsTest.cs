using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

public class GetServiceProviderTermsTest : IntegrationTestBase, IClassFixture<IntegrationTestFixture>
{
    private readonly Api _api;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetServiceProviderTermsTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = Fixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _api = Fixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task WhenCallingGetServiceProviderTermsEndpointThenReturnOkResponse()
    {
        var consentReceiverOrganization = Any.Organization();
        var user = Any.User();
        var organization = Any.Organization();
        organization.AcceptServiceProviderTerms();
        var affiliation = Affiliation.Create(user, organization);
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(consentReceiverOrganization);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        var api = Fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value, orgId: organization.Id.ToString());
        var response = await api.GetServiceProviderTerms();
        response.Should().Be200Ok();
    }
}
