using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetServiceProviderTermsTest : IntegrationTestBase
{
    public GetServiceProviderTermsTest(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GivenKnownOrganizationId_WhenGrantingConsent_200OkReturned()
    {
        var consentReceiverOrganization = Any.Organization();
        var user = Any.User();
        var organization = Any.Organization();
        organization.AcceptServiceProviderTerms();
        var affiliation = Affiliation.Create(user, organization);
        await _fixture.DbContext.Organizations.AddAsync(consentReceiverOrganization);
        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Affiliations.AddAsync(affiliation);
        await _fixture.DbContext.SaveChangesAsync();

        var api = _fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value, orgId: organization.Id.ToString());
        var response = await api.GetServiceProviderTerms();
        response.Should().Be200Ok();
    }
}
