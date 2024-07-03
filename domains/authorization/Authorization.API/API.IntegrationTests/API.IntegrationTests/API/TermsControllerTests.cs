using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class TermsControllerTests
{
    private readonly Api _api;
    private readonly IntegrationTestFixture _integrationTestFixture;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TermsControllerTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo).Options;

        _integrationTestFixture = integrationTestFixture;
        _api = integrationTestFixture.WebAppFactory.CreateApi();
    }

    [Fact]
    public async Task GivenValidOrganization_WhenAcceptingTerms_ThenReturnsAcceptedTrue()
    {
        var (idpUserId, tin) = await SeedData(termsAccepted: true, termsVersion: "1.0");

        var azureClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: idpUserId.Value.ToString(), orgCvr: tin.Value);

        var acceptTermsDto = new AcceptTermsDto(
            Tin: tin.Value,
            OrganizationName: "Test Org",
            UserIdpId: idpUserId.Value,
            UserName: "Test User",
            TermsVersion: "1.0"
        );

        var response = await azureClient.AcceptTerms(acceptTermsDto);

        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<TermsResponseDto>();
        result!.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNoOrganization_WhenAcceptingTerms_ThenReturnsLatestTermsForAcceptance()
    {
        var acceptTermsDto = new AcceptTermsDto(
            Tin: "12345678",
            OrganizationName: "Test Org",
            UserIdpId: Guid.NewGuid(),
            UserName: "Test User",
            TermsVersion: "1.0"
        );

        var response = await _api.AcceptTerms(acceptTermsDto);

        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<TermsResponseDto>();
        result!.Accepted.Should().BeFalse();
        result.TermsText.Should().NotBeNullOrEmpty();
        result.TermsVersion.Should().Be("1.0");
    }

    [Fact]
    public async Task GivenOutdatedTerms_WhenAcceptingTerms_ThenReturnsLatestTermsForAcceptance()
    {
        var (idpUserId, tin) = await SeedData(termsAccepted: true, termsVersion: "0.9");

        var azureClient = _integrationTestFixture.WebAppFactory.CreateApi(sub: idpUserId.Value.ToString(), orgCvr: tin.Value);

        var acceptTermsDto = new AcceptTermsDto(
            Tin: tin.Value,
            OrganizationName: "Test Org",
            UserIdpId: idpUserId.Value,
            UserName: "Test User",
            TermsVersion: "0.9"
        );

        var response = await azureClient.AcceptTerms(acceptTermsDto);

        response.Should().Be200Ok();
        var result = await response.Content.ReadFromJsonAsync<TermsResponseDto>();
        result!.Accepted.Should().BeFalse();
        result.TermsText.Should().NotBeNullOrEmpty();
        result.TermsVersion.Should().Be("1.0");
    }

    private async Task<(IdpUserId idpUserId, Tin tin)> SeedData(bool termsAccepted = false, string termsVersion = "1.0")
    {
        var user = Any.User();
        var organization = Any.Organization();
        if (termsAccepted)
        {
            organization.AcceptTerms(new Terms(termsVersion));
        }
        var affiliation = Affiliation.Create(user, organization);

        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Users.AddAsync(user);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Affiliations.AddAsync(affiliation);

        await dbContext.SaveChangesAsync();
        return (user.IdpUserId, organization.Tin);
    }
}
