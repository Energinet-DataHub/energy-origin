using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

public class DeleteConsentTests : IntegrationTestBase, IAsyncLifetime
{

    public DeleteConsentTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GivenValidConsent_WhenDeletingConsent_ThenHttp204NoContent()
    {
        // Given
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);
        var organizationWithClient = Any.OrganizationWithClient();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Affiliations.AddAsync(affiliation);
        await _fixture.DbContext.Organizations.AddAsync(organizationWithClient);
        await _fixture.DbContext.OrganizationConsents.AddAsync(consent);
        await _fixture.DbContext.SaveChangesAsync();

        var userClient = _fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);

        // When
        var response = await userClient.DeleteConsent(consent.Id);
        var deletedConsent = await _fixture.DbContext.OrganizationConsents.FirstOrDefaultAsync(x => x.Id == consent.Id)!;

        // Then
        response.Should().Be204NoContent();
        deletedConsent.Should().BeNull();
    }

    [Fact]
    public async Task GivenValidConsent_WhenDeletingConsentAsNonAffiliatedUser_ThenHttp404NotFound()
    {
        // Given
        var user = Any.User();
        var userOrganization = Any.Organization();
        var affiliation = Affiliation.Create(user, userOrganization);
        var consentGiverOrganization = Any.Organization();
        var consentReceiverOrganization = Any.Organization();
        var consent = OrganizationConsent.Create(consentGiverOrganization.Id, consentReceiverOrganization.Id, DateTimeOffset.UtcNow);

        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddAsync(userOrganization);
        await _fixture.DbContext.Organizations.AddAsync(consentGiverOrganization);
        await _fixture.DbContext.Affiliations.AddAsync(affiliation);
        await _fixture.DbContext.Organizations.AddAsync(consentReceiverOrganization);
        await _fixture.DbContext.OrganizationConsents.AddAsync(consent);
        await _fixture.DbContext.SaveChangesAsync();

        var userClient = _fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: userOrganization.Tin!.Value);

        // When
        var response = await userClient.DeleteConsent(consent.Id);

        // Then
        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenNonExistingConsent_WhenDeletingConsent_ThenHttp404NotFound()
    {
        var user = Any.User();
        var organization = Any.Organization();
        var affiliation = Affiliation.Create(user, organization);

        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Affiliations.AddAsync(affiliation);
        await _fixture.DbContext.SaveChangesAsync();

        var userClient = _fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);

        var randomGuidClientId = Guid.NewGuid();

        var response = await userClient.DeleteConsent(randomGuidClientId);

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GivenUserAffiliatedWithMultipleOrganizationsButLoggedInOnBehalfOfASingleOne_WhenDeletingConsent_ThenHttp404NotFoundIfNotLoggedInOnBehalfOfTheOwningOrganization()
    {
        // Given
        var user = Any.User();

        var organization1 = Any.Organization();
        var organization2 = Any.Organization();

        var organizationWithClient1 = Any.OrganizationWithClient();
        var organizationWithClient2 = Any.OrganizationWithClient();

        var consent1 = OrganizationConsent.Create(organization1.Id, organizationWithClient1.Id, DateTimeOffset.UtcNow);
        var consent2 = OrganizationConsent.Create(organization2.Id, organizationWithClient2.Id, DateTimeOffset.UtcNow);

        var affiliation1 = Affiliation.Create(user, organization1);
        var affiliation2 = Affiliation.Create(user, organization2);

        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddRangeAsync([organization1, organization2, organizationWithClient1, organizationWithClient2]);
        await _fixture.DbContext.Affiliations.AddRangeAsync([affiliation1, affiliation2]);

        await _fixture.DbContext.OrganizationConsents.AddRangeAsync([consent1, consent2]);

        await _fixture.DbContext.SaveChangesAsync();

        var userIdString = user.IdpUserId.Value.ToString();

        var userClient = _fixture.WebAppFactory.CreateApi(sub: userIdString, orgCvr: organization1.Tin!.Value);

        // When
        var response = await userClient.DeleteConsent(consent2.Id);

        // Then
        response.Should().Be404NotFound();
    }


    [Fact]
    public async Task GivenConsent_WhenDeleted_ThenAccessTokenShouldNotContainOrganizationId()
    {
        // Given
        var user = Any.User();
        var organization = Any.Organization();
        var organizationWithClient = Any.OrganizationWithClient();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);
        var affiliation = Affiliation.Create(user, organization);


        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Organizations.AddAsync(organizationWithClient);
        await _fixture.DbContext.OrganizationConsents.AddAsync(consent);
        await _fixture.DbContext.Affiliations.AddAsync(affiliation);
        await _fixture.DbContext.SaveChangesAsync();

        var userClient = _fixture.WebAppFactory.CreateApi(sub: user.IdpUserId.Value.ToString(), orgCvr: organization.Tin!.Value);
        var consentListResponse = await userClient.GetUserOrganizationConsents();
        consentListResponse.Should().Be200Ok();

        var consentList = await consentListResponse.Content.ReadFromJsonAsync<UserOrganizationConsentsResponse>();
        consentList!.Result.Should().NotBeEmpty();

        // When
        var deleteResponse = await userClient.DeleteConsent(consent.Id);
        deleteResponse.Should().Be204NoContent();

        // Then
        var consentListResponseAfterDeletion = await userClient.GetUserOrganizationConsents();
        consentListResponseAfterDeletion.Should().Be200Ok();
        var consentListAfterDeletion = await consentListResponseAfterDeletion.Content.ReadFromJsonAsync<UserOrganizationConsentsResponse>();
        consentListAfterDeletion!.Result.Should().BeEmpty();
    }
}
