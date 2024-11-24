using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Controllers;

public class TermsControllerTests : IntegrationTestBase, IAsyncLifetime
{

    public TermsControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GivenValidRequest_WhenAcceptingTerms_ThenHttpOkAndTermsAccepted()
    {

        if (!_fixture.DbContext.Terms.Any())
        {
            _fixture.DbContext.Terms.Add(Terms.Create(1));
            _fixture.DbContext.SaveChanges();
        }


        var terms = _fixture.DbContext.Terms.First();
        var orgCvr = Tin.Create("12345678");

        var userApi = _fixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: orgCvr.Value, termsAccepted: false);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var organization = await _fixture.DbContext.Organizations.FirstOrDefaultAsync(o => o.Tin == orgCvr);
        organization.Should().NotBeNull();
        organization!.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(terms.Version);
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenAcceptingTerms_ThenHttpOkAndTermsUpdated()
    {
        var terms = await _fixture.DbContext.Terms.FirstAsync();

        var orgCvr = Any.Tin();
        var organization = Organization.Create(orgCvr, OrganizationName.Create("Existing Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var userApi = _fixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: organization.Tin!.Value, termsAccepted: false);

        var response = await userApi.AcceptTerms();

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Terms accepted successfully.");

        var updatedOrganization = await _fixture.DbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Tin == orgCvr);

        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.TermsAccepted.Should().BeTrue();
        updatedOrganization.TermsVersion.Should().Be(terms.Version);
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenRevokingTerms_ThenHttpOkAndTermsUpdated()
    {
        if (!_fixture.DbContext.Terms.Any())
        {
            _fixture.DbContext.Terms.Add(Terms.Create(1));
            _fixture.DbContext.SaveChanges();
        }

        var terms = _fixture.DbContext.Terms.First();
        var orgCvr = Any.Tin();

        var organization = Organization.Create(orgCvr, OrganizationName.Create("Existing Org"));
        organization.AcceptTerms(terms);
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var userApi = _fixture.WebAppFactory.CreateApi(sub: Any.Guid().ToString(), orgCvr: organization.Tin!.Value, orgId: organization.Id.ToString(), termsAccepted: true);

        // When
        var response = await userApi.RevokeTerms();

        // Then
        var updatedOrganization = await _fixture.DbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Tin == orgCvr);

        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<RevokeTermsResponse>();
        result!.Message.Should().Be("Terms revoked successfully.");
        updatedOrganization!.TermsAccepted.Should().BeFalse();
        updatedOrganization.TermsVersion.Should().BeNull();
        updatedOrganization.TermsAcceptanceDate.Should().BeNull();
    }

    private async Task SeedOrganizationAndUser(Organization organization, User user)
    {
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Users.AddAsync(user);
        await _fixture.DbContext.SaveChangesAsync();
    }
}
