using System.Net.Http.Json;
using API.Authorization._Features_;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class DeleteConsentTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public DeleteConsentTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenValidConsent_WhenDeletingConsent_ThenHttp204NoContent()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        var client = Any.Client();
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Consents.AddAsync(consent);
        await dbContext.SaveChangesAsync();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);

        var response = await userClient.DeleteConsent(client.IdpClientId.Value);

        response.Should().Be204NoContent();

        var deletedConsent = await dbContext.Consents
            .FirstOrDefaultAsync(c => c.ClientId == client.Id && c.OrganizationId == organization.Id);

        deletedConsent.Should().BeNull();
    }

    [Fact]
    public async Task GivenNonExistingConsent_WhenDeletingConsent_ThenHttp404NotFound()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);

        var randomGuidClientId = Guid.NewGuid();

        var response = await userClient.DeleteConsent(randomGuidClientId);

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenUserAffiliatedWithMultipleOrganizationsButLoggedInOnBehalfOfASingleOne_WhenDeletingConsent_ThenHttp403ForbiddenIfNotLoggedInOnBehalfOfTheOwningOrganization()
    {
        var user = Any.User();

        var organization1 = Any.Organization();
        var organization2 = Any.Organization();

        var client1 = Any.Client();
        var client2 = Any.Client();

        var consent1 = Consent.Create(organization1, client1, DateTimeOffset.UtcNow);
        var consent2 = Consent.Create(organization2, client2, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddRangeAsync([organization1, organization2]);
        await dbContext.Clients.AddRangeAsync([client1, client2]);

        var affiliation1 = Affiliation.Create(user, organization1);
        var affiliation2 = Affiliation.Create(user, organization2);

        await dbContext.Affiliations.AddRangeAsync([affiliation1, affiliation2]);
        await dbContext.Consents.AddRangeAsync([consent1, consent2]);

        await dbContext.SaveChangesAsync();

        var userIdString = user.IdpUserId.Value.ToString();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: userIdString, orgCvr: organization1.Tin!.Value);

        var response = await userClient.DeleteConsent(client2.IdpClientId.Value);

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenConsent_WhenDeleted_ThenAccessTokenShouldNotContainOrganizationId()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var client = Client.Create(new IdpClientId(Guid.NewGuid()), new("Loz"), ClientType.Internal, "https://localhost:5001");
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Clients.AddAsync(client);
        await dbContext.Consents.AddAsync(consent);
        var affiliation = Affiliation.Create(user, organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.SaveChangesAsync();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);
        var consentListResponse = await userClient.GetUserOrganizationConsents();
        consentListResponse.Should().Be200Ok();
        var consentList = await consentListResponse.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();
        consentList!.Result.Should().NotBeEmpty();

        var deleteResponse = await userClient.DeleteConsent(client.IdpClientId.Value);
        deleteResponse.Should().Be204NoContent();

        var consentListResponseAfterDeletion = await userClient.GetUserOrganizationConsents();
        consentListResponseAfterDeletion.Should().Be200Ok();
        var consentListAfterDeletion = await consentListResponseAfterDeletion.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();
        consentListAfterDeletion!.Result.Should().BeEmpty();
    }
}
