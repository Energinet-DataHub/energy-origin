using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Models.ClientType;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetClientGrantedConsentsQueryTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly Guid _sub;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetClientGrantedConsentsQueryTest(IntegrationTestFixture integrationTestFixture) : base(integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        _sub = Guid.NewGuid();
        _api = integrationTestFixture.WebAppFactory.CreateApi(_sub.ToString());
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_ThenClientConsentsReturned()
    {
        // Given
        await using var dbContext = new ApplicationDbContext(_options);

        var client = Client.Create(new IdpClientId(_sub), new("Loz"), ClientType.Internal, "https://localhost:5001", false);
        var organization = Any.Organization();
        var organizationWithClient = Any.OrganizationWithClient(client: client);
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organizationWithClient, TestContext.Current.CancellationToken);
        await dbContext.OrganizationConsents.AddAsync(consent, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // When
        var response = await _api.GetClientConsents();
        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>(TestContext.Current.CancellationToken);

        // Then
        response.Should().Be200Ok();
        result!.Result.Count().Should().Be(1);
        result!.Result.First().OrganizationName.Should().Be(organization.Name.Value);
        result!.Result.First().Tin.Should().Be(organization.Tin!.Value);
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_WithNoConsents_ThenEmptyResponseReturned()
    {
        // Given
        await using var dbContext = new ApplicationDbContext(_options);

        var organizationWithClient = Any.OrganizationWithClient();
        var organization = Any.Organization();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await dbContext.Organizations.AddAsync(organizationWithClient, TestContext.Current.CancellationToken);
        await dbContext.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await dbContext.OrganizationConsents.AddAsync(consent, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // When
        var response = await _api.GetClientConsents();
        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>(TestContext.Current.CancellationToken);

        // Then
        response.Should().Be200Ok();
        result!.Result.Should().BeEmpty();
    }

}
