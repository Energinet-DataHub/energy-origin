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
public class GetClientConsentsQueryTest
{
    private readonly Api _api;
    private readonly Guid _sub;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public GetClientConsentsQueryTest(IntegrationTestFixture integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        _sub = Guid.NewGuid();
        _api = integrationTestFixture.WebAppFactory.CreateApi(_sub.ToString());
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_ThenClientConsentsReturned()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(_options);

        var client = Client.Create(new IdpClientId(_sub), new ("Loz"), ClientType.Internal, "https://localhost:5001");
        var organization = Any.Organization();
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await dbContext.Clients.AddAsync(client);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Consents.AddAsync(consent);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _api.GetClientConsents();
        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>();

        // Assert
        result!.Items.Count().Should().Be(1);
        result!.Items.First().OrganizationName.Should().Be(organization.Name.Value);
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_WithNoConsents_ThenEmptyResponseReturned()
    {
        // Arrange
        await using var dbContext = new ApplicationDbContext(_options);

        var client = Any.Client();
        var organization = Any.Organization();
        var consent = Consent.Create(organization, client, DateTimeOffset.UtcNow);

        await dbContext.Clients.AddAsync(client);
        await dbContext.Organizations.AddAsync(organization);
        await dbContext.Consents.AddAsync(consent);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _api.GetClientConsents();
        response.Should().Be200Ok();

        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>();

        // Assert
        result!.Items.Should().BeEmpty();
    }
}
