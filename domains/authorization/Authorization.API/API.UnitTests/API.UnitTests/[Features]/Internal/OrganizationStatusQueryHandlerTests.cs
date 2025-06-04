using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class GetOrganizationStatusQueryHandlerTests
{
    private readonly FakeOrganizationRepository _fakeOrgRepo;
    private readonly FakeWhitelistedRepository _fakeWhitelistRepo;
    private readonly GetOrganizationStatusQueryHandler _sut;

    public GetOrganizationStatusQueryHandlerTests()
    {
        _fakeOrgRepo = new FakeOrganizationRepository();
        _fakeWhitelistRepo = new FakeWhitelistedRepository();
        _sut = new GetOrganizationStatusQueryHandler(_fakeOrgRepo, _fakeWhitelistRepo);
    }

    [Fact]
    public async Task OrganizationDoesNotExist_NotWhitelisted_ReturnsTrue()
    {
        // Arrange
        var query = new GetOrganizationStatusQuery("12345678");
        // No org added, no whitelist added

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result);  // Null org + NOT whitelisted => true (trial)
    }

    [Fact]
    public async Task OrganizationDoesNotExist_Whitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(tin), CancellationToken.None);

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Null org + whitelisted => false (normal)
    }

    [Fact]
    public async Task TrialOrganization_NotWhitelisted_ReturnsTrue()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.CreateTrial(tin, OrganizationName.Create("Trial Org"));
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        // Not whitelisted

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result);  // Trial org + NOT whitelisted => true (trial)
    }

    [Fact]
    public async Task TrialOrganization_Whitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.CreateTrial(tin, OrganizationName.Create("Trial Org"));
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(tin), CancellationToken.None);

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Trial org + whitelisted => false (normal)
    }

    [Fact]
    public async Task NormalOrganization_NotWhitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.Create(tin, OrganizationName.Create("Normal Org"));
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        // Not whitelisted

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Normal org + NOT whitelisted => false (normal)
    }

    [Fact]
    public async Task NormalOrganization_Whitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.Create(tin, OrganizationName.Create("Normal Org"));
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(tin), CancellationToken.None);

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Normal org + whitelisted => false (normal)
    }

    [Fact]
    public async Task DeactivatedOrganization_NotWhitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.Create(tin, OrganizationName.Create("Deactivated Org"));
        org.Deactivate();
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        // Not whitelisted

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Deactivated org + NOT whitelisted => false (normal)
    }

    [Fact]
    public async Task DeactivatedOrganization_Whitelisted_ReturnsFalse()
    {
        // Arrange
        var tin = Tin.Create("12345678");
        var org = Organization.Create(tin, OrganizationName.Create("Deactivated Org"));
        org.Deactivate();
        await _fakeOrgRepo.AddAsync(org, CancellationToken.None);
        await _fakeWhitelistRepo.AddAsync(Whitelisted.Create(tin), CancellationToken.None);

        var query = new GetOrganizationStatusQuery("12345678");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);  // Deactivated org + whitelisted => false (normal)
    }
}
