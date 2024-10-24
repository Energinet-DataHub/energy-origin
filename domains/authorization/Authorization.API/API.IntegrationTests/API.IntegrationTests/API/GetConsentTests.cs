using System.Net.Http.Json;
using API.Authorization._Features_;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetConsentTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetConsentTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenUser_WhenGettingConsent_ThenHttpOkConsentReturned()
    {
        // Given
        var (idpUserId, tin) = await SeedData();

        // When
        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: idpUserId.Value.ToString(), orgCvr: tin.Value);
        var response = await userClient.GetUserOrganizationConsents();

        // Then
        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();
        result!.Result.Should().NotBeEmpty();
        var firstResult = result.Result.First();
        firstResult.ConsentId.Should().NotBeEmpty();
        firstResult.GiverOrganizationId.Should().NotBeEmpty();
        firstResult.GiverOrganizationName.Should().NotBeEmpty();
        firstResult.ReceiverOrganizationId.Should().NotBeEmpty();
        firstResult.ReceiverOrganizationName.Should().NotBeEmpty();
        firstResult.ConsentDate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GivenNoConsentsExist_WhenUserQueriesForConsents_HttpOKEmptyBody()
    {
        await SeedData();

        var response = await _api.GetUserOrganizationConsents();

        response.Should().Be200Ok();

        var deserializedResponse = await response.Content.ReadFromJsonAsync<UserOrganizationConsentsResponse>();

        deserializedResponse!.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task
        GivenUserAffiliatedWithMultipleOrganizations_WhenGettingConsent_ThenOnlyConsentFromCurrentOrganizationContextIncludedInResponse()
    {
        // Given
        var user = Any.User();
        var organization1 = Any.Organization();
        var organization2 = Any.Organization();
        var organizationWithClient1 = Any.OrganizationWithClient();
        var organizationWithClient2 = Any.OrganizationWithClient();
        var consent1 = OrganizationConsent.Create(organization1.Id, organizationWithClient1.Id, DateTimeOffset.UtcNow);
        var consent2 = OrganizationConsent.Create(organization2.Id, organizationWithClient2.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddRangeAsync([organization1, organization2, organizationWithClient1, organizationWithClient2]);

        var affiliation1 = Affiliation.Create(user, organization1);
        var affiliation2 = Affiliation.Create(user, organization2);

        await dbContext.Affiliations.AddRangeAsync([affiliation1, affiliation2]);
        await dbContext.OrganizationConsents.AddRangeAsync([consent1, consent2]);

        await dbContext.SaveChangesAsync();

        // When
        var userIdString = user.IdpUserId.Value.ToString();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: userIdString, orgCvr: organization1.Tin!.Value);
        var response = await userClient.GetUserOrganizationConsents();

        // Then
        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();

        result!.Result.Count.Should().Be(1);
        var firstResult = result.Result.First();
        firstResult.GiverOrganizationId.Should().Be(organization1.Id);
        firstResult.GiverOrganizationName.Should().Be(organization1.Name.Value);
        firstResult.ReceiverOrganizationId.Should().Be(organizationWithClient1.Id);
        firstResult.ReceiverOrganizationName.Should().Be(organizationWithClient1.Name.Value);
    }

    [Fact]
    public async Task GivenTwoDistinctUsersAssociatedWithSameOrganization_WhenQueryingForConsents_ThenReturnTheSameConsentResponse()
    {
        var user1 = Any.User();
        var user2 = Any.User();
        var organization = Any.Organization();
        var organizationWithClient = Any.OrganizationWithClient();

        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddRangeAsync(user1, user2);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Organizations.AddAsync(organizationWithClient);

        var affiliation1 = Affiliation.Create(user1, organization);
        var affiliation2 = Affiliation.Create(user2, organization);

        await dbContext.Affiliations.AddRangeAsync([affiliation1, affiliation2]);
        await dbContext.OrganizationConsents.AddAsync(consent);

        await dbContext.SaveChangesAsync();

        var userClient1 = _integrationTestFixture.WebAppFactory.CreateApi(sub: user1.IdpUserId.Value.ToString());
        var response1 = await userClient1.GetUserOrganizationConsents();

        var userClient2 = _integrationTestFixture.WebAppFactory.CreateApi(sub: user2.IdpUserId.Value.ToString());
        var response2 = await userClient2.GetUserOrganizationConsents();

        response1.Should().Be200Ok();
        response2.Should().Be200Ok();

        var result1 = await response1.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();
        var result2 = await response2.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>();

        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task GivenOrganizationWithGivenAndReceivedConsents_WhenGettingConsents_ResultIncludeBothGivenAndReceivedConsents()
    {
        // Given
        var user = Any.User();
        var userOrganization = Any.Organization();
        var organization1 = Any.Organization();
        var organization2 = Any.Organization();
        var consent1 = OrganizationConsent.Create(userOrganization.Id, organization1.Id, DateTimeOffset.UtcNow);
        var consent2 = OrganizationConsent.Create(organization2.Id, userOrganization.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddRangeAsync([userOrganization, organization1, organization2]);

        var affiliation = Affiliation.Create(user, userOrganization);

        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.OrganizationConsents.AddRangeAsync([consent1, consent2]);

        await dbContext.SaveChangesAsync();

        // When
        var userIdString = user.IdpUserId.Value.ToString();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: userIdString, orgCvr: userOrganization.Tin!.Value);
        var response = await userClient.GetUserOrganizationConsents();

        // Then
        response.Should().Be200Ok();

        var result = (await response.Content.ReadFromJsonAsync<GetUserOrganizationConsentsQueryResult>())!;

        result.Result.Count.Should().Be(2);
        result.Result.Should().Contain(c => c.GiverOrganizationId == userOrganization.Id && c.ReceiverOrganizationId == organization1.Id);
        result.Result.Should().Contain(c => c.ReceiverOrganizationId == userOrganization.Id && c.GiverOrganizationId == organization2.Id);
    }

    [Fact]
    public async Task GivenOrganizationWithGivenAndReceivedConsents_WhenGettingReceivedConsents_ResultIncludeBothGivenAndReceivedConsents()
    {
        // Given
        var user = Any.User();
        var userOrganization = Any.Organization();
        var organization1 = Any.Organization();
        var organization2 = Any.Organization();
        var consent1 = OrganizationConsent.Create(userOrganization.Id, organization1.Id, DateTimeOffset.UtcNow);
        var consent2 = OrganizationConsent.Create(organization2.Id, userOrganization.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);

        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddRangeAsync([userOrganization, organization1, organization2]);

        var affiliation = Affiliation.Create(user, userOrganization);

        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.OrganizationConsents.AddRangeAsync([consent1, consent2]);

        await dbContext.SaveChangesAsync();

        // When
        var userIdString = user.IdpUserId.Value.ToString();

        var userClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: userIdString, orgCvr: userOrganization.Tin!.Value);
        var response = await userClient.GetUserOrganizationReceivedConsents();

        // Then
        response.Should().Be200Ok();

        var result = (await response.Content.ReadFromJsonAsync<UserOrganizationConsentsReceivedResponse>())!;

        result.Result.Count().Should().Be(1);
        result.Result.Should().Contain(c => c.OrganizationId == organization2.Id);
        result.Result.Should().Contain(c => c.OrganizationName == organization2.Name.Value);
    }

    private async Task<(IdpUserId, Tin)> SeedData()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        var organizationWithClient = Any.OrganizationWithClient();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);
        await dbContext.Organizations.AddAsync(organizationWithClient);
        await dbContext.OrganizationConsents.AddAsync(consent);

        await dbContext.SaveChangesAsync();
        return (user.IdpUserId, organization.Tin!);
    }
}
