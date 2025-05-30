using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using EnergyOrigin.TokenValidation.b2c;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GrantConsentTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GrantConsentTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenGrantingConsent_HttpNotFoundIsReturned()
    {
        var unknownClientId = Guid.NewGuid();
        var response = await _api.GrantConsentToClient(unknownClientId);
        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenUnknownOrganizationId_WhenGrantingConsent_HttpNotFoundIsReturned()
    {
        var unknownOrganizationId = Guid.NewGuid();
        var response = await _api.GrantConsentToOrganization(unknownOrganizationId);
        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenKnownClientId_WhenGrantingConsent_200OkReturned()
    {
        var organizationWithClient = Any.OrganizationWithClient();
        var user = Any.User();
        var organizationThatIsGrantingConsent = Any.Organization();
        organizationWithClient.AcceptServiceProviderTerms();
        var affiliation = Affiliation.Create(user, organizationThatIsGrantingConsent);
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organizationWithClient, TestContext.Current.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organizationThatIsGrantingConsent, TestContext.Current.CancellationToken);
        await dbContext.Affiliations.AddAsync(affiliation, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var api = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organizationThatIsGrantingConsent.Tin!.Value);
        var response = await api.GrantConsentToClient(organizationWithClient.Clients.First().IdpClientId.Value);
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenKnownClientId_WhenGrantingConsentTwice_200OkReturned()
    {
        var organizationWithClient = Any.OrganizationWithClient();
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organizationWithClient, TestContext.Current.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Affiliations.AddAsync(affiliation, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var api = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);
        var response = await api.GrantConsentToClient(organizationWithClient.Clients.First().IdpClientId.Value);
        response.Should().Be200Ok();

        var response2 = await api.GrantConsentToClient(organizationWithClient.Clients.First().IdpClientId.Value);
        response2.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenKnownOrganizationId_WhenGrantingConsent_200OkReturned()
    {
        var consentReceiverOrganization = Any.Organization();
        consentReceiverOrganization.AcceptServiceProviderTerms();
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(consentReceiverOrganization, TestContext.Current.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Affiliations.AddAsync(affiliation, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var api = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);
        var response = await api.GrantConsentToOrganization(consentReceiverOrganization.Id);
        response.Should().Be200Ok();
    }

    [Fact]
    public async Task GivenKnownOrganizationId_WhenGrantingConsentToReceiverOrganizationWithoutServiceProviderTerms_Return403Forbidden()
    {
        var consentReceiverOrganization = Any.Organization();
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(consentReceiverOrganization, TestContext.Current.CancellationToken);
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Affiliations.AddAsync(affiliation, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var api = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);
        var response = await api.GrantConsentToOrganization(consentReceiverOrganization.Id);
        response.Should().Be403Forbidden();
    }

    [Fact]
    public async Task GivenSubTypeNotUser_WhenGrantingConsent_HttpForbiddenIsReturned()
    {
        var api = _integrationTestFixture.WebAppFactory.CreateApi(subType: SubjectType.External.ToString(), termsAccepted: false);

        var response = await api.GrantConsentToClient(Guid.NewGuid());

        response.Should().Be403Forbidden();
    }
}
