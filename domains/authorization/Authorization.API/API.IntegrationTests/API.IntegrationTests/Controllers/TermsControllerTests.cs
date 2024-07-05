using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class AcceptTermsTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public AcceptTermsTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenValidRequest_WhenAcceptingTerms_ThenHttpOkAndTermsAccepted()
    {
        var terms = Terms.Create("1.0");
        await SeedTerms(terms);

        var request = new AcceptTermsRequest(
            OrgCvr: "12345678",
            UserId: Guid.NewGuid(),
            UserName: "Test User",
            OrganizationName: "Test Org"
        );

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: request.UserId.ToString(), orgCvr: request.OrgCvr);

        var response = await userApi.AcceptTerms(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponseDto>();
        result.Should().NotBeNull();
        result!.Status.Should().BeTrue();
        result.Message.Should().Be("Terms accepted successfully.");

        await using var dbContext = new ApplicationDbContext(_options);
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Tin.Value == request.OrgCvr);
        organization.Should().NotBeNull();
        organization!.TermsAccepted.Should().BeTrue();
        organization.TermsVersion.Should().Be(terms.Version);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.IdpUserId.Value == request.UserId);
        user.Should().NotBeNull();

        var affiliation = await dbContext.Affiliations
            .FirstOrDefaultAsync(a => a.User.IdpUserId.Value == request.UserId && a.Organization.Tin.Value == request.OrgCvr);
        affiliation.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenNoTermsExist_WhenAcceptingTerms_ThenHttpBadRequest()
    {
        var request = new AcceptTermsRequest(
            OrgCvr: "12345678",
            UserId: Guid.NewGuid(),
            UserName: "Test User",
            OrganizationName: "Test Org"
        );

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: request.UserId.ToString(), orgCvr: request.OrgCvr);

        var response = await userApi.AcceptTerms(request);

        response.Should().Be400BadRequest();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponseDto>();
        result.Should().NotBeNull();
        result!.Status.Should().BeFalse();
        result.Message.Should().Be("Failed to accept terms.");
    }

    [Fact]
    public async Task GivenExistingOrganizationAndUser_WhenAcceptingTerms_ThenHttpOkAndTermsUpdated()
    {
        var terms = Terms.Create("1.0");
        await SeedTerms(terms);

        var organization = Organization.Create(new Tin("12345678"), new OrganizationName("Existing Org"));
        var user = User.Create(IdpUserId.Create(Guid.NewGuid()), UserName.Create("Existing User"));
        await SeedOrganizationAndUser(organization, user);

        var request = new AcceptTermsRequest(
            OrgCvr: organization.Tin.Value,
            UserId: user.IdpUserId.Value,
            UserName: user.Name.Value,
            OrganizationName: organization.Name.Value
        );

        var userApi = _integrationTestFixture.WebAppFactory.CreateApi(sub: request.UserId.ToString(), orgCvr: request.OrgCvr);

        var response = await userApi.AcceptTerms(request);

        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<AcceptTermsResponseDto>();
        result.Should().NotBeNull();
        result!.Status.Should().BeTrue();
        result.Message.Should().Be("Terms accepted successfully.");

        await using var dbContext = new ApplicationDbContext(_options);
        var updatedOrganization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.Tin.Value == request.OrgCvr);
        updatedOrganization.Should().NotBeNull();
        updatedOrganization!.TermsAccepted.Should().BeTrue();
        updatedOrganization.TermsVersion.Should().Be(terms.Version);

        var affiliation = await dbContext.Affiliations
            .FirstOrDefaultAsync(a => a.User.IdpUserId.Value == request.UserId && a.Organization.Tin.Value == request.OrgCvr);
        affiliation.Should().NotBeNull();
    }

    private async Task SeedTerms(Terms terms)
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Terms.AddAsync(terms);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedOrganizationAndUser(Organization organization, User user)
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}
