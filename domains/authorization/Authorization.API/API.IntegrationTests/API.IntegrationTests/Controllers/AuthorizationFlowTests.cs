using API.Authorization._Features_;
using API.Authorization._Features_.Internal;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Metrics;
using API.Models;
using API.Repository;
using API.UnitTests;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AuthorizationFlowTests : IntegrationTestBase
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AuthorizationFlowTests(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;
        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi(sub: _integrationTestFixture.WebAppFactory
            .IssuerIdpClientId.ToString());
    }

    [Fact]
    public async Task GivenNonExistingUserAndNoneExistingOrganization_WhenGoingThroughAcceptTermsFlow_ThenOrginizationAffiliationAndUserIsCreated()
    {
        // Given
        var user = new
        {
            OrgCvr = "87654321",
            OrgName = Guid.NewGuid().ToString(),
            Name = Guid.NewGuid().ToString(),
            Sub = Guid.NewGuid()
        };

        await using var dbContext = new ApplicationDbContext(_options);

        var request = new AuthorizationUserRequest(
            Sub: user.Sub,
            Name: user.Name,
            OrgCvr: user.OrgCvr,
            OrgName: user.OrgName
        );

        var consentResponse1 = await _api.GetConsentForUser(request);

        // When
        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.Sub.ToString(), orgCvr: user.OrgCvr,
            orgName: user.OrgName, termsAccepted: false);
        var termsResponse = await userApi.AcceptTerms();
        var consentResponse2 = await _api.GetConsentForUser(request);

        // Then
        consentResponse1.Should().Be200Ok();
        termsResponse.Should().Be200Ok();
        consentResponse2.Should().Be200Ok();

        var organization = dbContext.Organizations
            .Include(x => x.Affiliations)
            .ThenInclude(y => y.User).ToList()
            .Single(x => x.Tin == Tin.Create(user.OrgCvr));

        organization.Name.Value.Should().Be(user.OrgName);
        organization.Affiliations.Should().ContainSingle();
        organization.Affiliations.Single().User.Name.Value.Should().Be(user.Name);
    }

    [Fact]
    public async Task GivenNonExistingUserAndExistingOrganization_WhenGoingThroughAcceptTermsFlow_ThenAffiliationAndUserIsCreated()
    {
        // Given
        var user = new
        {
            OrgCvr = "11223344",
            OrgName = Guid.NewGuid().ToString(),
            Name = Guid.NewGuid().ToString(),
            Sub = Guid.NewGuid()
        };

        await using var dbContext = new ApplicationDbContext(_options);
        var org = Organization.Create(Tin.Create(user.OrgCvr), OrganizationName.Create(user.OrgName));
        org.AcceptTerms(dbContext.Terms.First());
        dbContext.Organizations.Add(org);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new AuthorizationUserRequest(
            Sub: user.Sub,
            Name: user.Name,
            OrgCvr: user.OrgCvr,
            OrgName: user.OrgName
        );

        // When
        var consentResponse = await _api.GetConsentForUser(request);

        // Then
        consentResponse.Should().Be200Ok();

        var organization = dbContext.Organizations
            .Include(x => x.Affiliations)
            .ThenInclude(y => y.User).ToList()
            .Single(x => x.Tin == Tin.Create(user.OrgCvr));

        organization.Name.Value.Should().Be(user.OrgName);
        organization.Affiliations.Should().ContainSingle();
        organization.Affiliations.Single().User.Name.Value.Should().Be(user.Name);
    }


    [Fact]
    public async Task GivenOrganizationWithReceivedConsents_WhenGettingAuthConsentClaimsAndOrganizationConsents_ThenBothListOfOrganizationsYouHaveAccessToIsTheSame()
    {
        // Given
        var user = Any.User();
        var organization = Any.Organization();
        var organization2 = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        var organizationWithClient = Any.OrganizationWithClient();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);
        var consent2 = OrganizationConsent.Create(organization2.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization2, TestContext.Current.CancellationToken);
        await dbContext.Affiliations.AddAsync(affiliation, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organizationWithClient, TestContext.Current.CancellationToken);
        await dbContext.OrganizationConsents.AddAsync(consent, TestContext.Current.CancellationToken);
        await dbContext.OrganizationConsents.AddAsync(consent2, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var getClientGrantedConsentsQueryHandler =
            new GetClientGrantedConsentsQueryHandler(new ClientRepository(dbContext));
        var consentForClientQueryHandler =
            new GetConsentForClientQueryHandler(new ClientRepository(dbContext), new AuthorizationMetrics());
        var idpClientId = organizationWithClient.Clients.First().IdpClientId;

        // When
        var getClientGrantedConsentsQueryResult =
            await getClientGrantedConsentsQueryHandler.Handle(new GetClientGrantedConsentsQuery(idpClientId),
                CancellationToken.None);
        var consentForClientQueryResult =
            await consentForClientQueryHandler.Handle(new GetConsentForClientQuery(idpClientId.Value),
                CancellationToken.None);

        // Then
        var grantedConsentsOrganizationIds =
            getClientGrantedConsentsQueryResult.GetClientConsentsQueryResultItems.Select(x => x.OrganizationId);
        var authorizationClaimOrganizationsIds = consentForClientQueryResult.OrgIds;

        grantedConsentsOrganizationIds.Should().BeEquivalentTo(authorizationClaimOrganizationsIds);
    }
}
