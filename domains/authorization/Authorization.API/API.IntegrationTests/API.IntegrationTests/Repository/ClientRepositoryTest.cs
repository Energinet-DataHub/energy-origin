using API.IntegrationTests.Setup;
using API.Models;
using API.Repository;
using API.UnitTests;
using API.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Repository;

[Collection(IntegrationTestCollection.CollectionName)]
public class ClientRepositoryTest
{
    private readonly ApplicationDbContext _db;

    public ClientRepositoryTest(IntegrationTestFixture integrationTestFixture)
    {
        var connectionString = integrationTestFixture.WebAppFactory.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _db = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GivenClient_ThatIsExternalAndHasAccessThroughOrganization_ReturnTrue()
    {
        // Arrange
        var client = Client.Create(Any.IdpClientId(), new ClientName("ClientName"), ClientType.External,
            "https://redirect.url");

        var organization = Any.OrganizationWithClient(client: client);
        await _db.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new ClientRepository(_db);

        // Act
        var hasAccess = await sut.ExternalClientHasAccessThroughOrganization(
            organization.Clients.First().IdpClientId.Value,
            organization.Id);

        // Assert
        Assert.True(hasAccess);
    }

    [Fact]
    public async Task GivenClient_ThatIsExternalAndDoesNotHaveAccessThroughOrganization_ReturnFalse()
    {
        // Arrange
        var client = Client.Create(Any.IdpClientId(), new ClientName("ClientName"), ClientType.External,
            "https://redirect.url");
        var clientTwo = Client.Create(Any.IdpClientId(), new ClientName("ClientName"), ClientType.External,
            "https://redirect.url");
        var organization = Any.OrganizationWithClient(client: client);
        var organizationTwo = Any.OrganizationWithClient(client: clientTwo);

        await _db.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await _db.Organizations.AddAsync(organizationTwo, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new ClientRepository(_db);

        // Act
        var hasAccess = await sut.ExternalClientHasAccessThroughOrganization(
            organizationTwo.Clients.First().IdpClientId.Value,
            organization.Id);

        // Assert
        Assert.False(hasAccess);
    }

    [Fact]
    public async Task GivenClient_ThatIsNotExternalAndHasAccessThroughOrganization_ReturnFalse()
    {
        // Arrange
        var client = Client.Create(Any.IdpClientId(), new ClientName("ClientName"), ClientType.Internal,
            "https://redirect.url");

        var organization = Any.OrganizationWithClient(client: client);
        await _db.Organizations.AddAsync(organization, TestContext.Current.CancellationToken);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new ClientRepository(_db);

        // Act
        var hasAccess = await sut.ExternalClientHasAccessThroughOrganization(
            organization.Clients.First().IdpClientId.Value,
            organization.Id);

        // Assert
        Assert.False(hasAccess);
    }
}
