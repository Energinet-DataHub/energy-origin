using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class GetOrganizationStatusQueryHandlerTests
{
    private readonly GetOrganizationStatusQueryHandler _sut;
    private readonly Organization _normalOrg;
    private readonly Organization _trialOrg;
    private readonly Organization _deactivatedOrg;

    public GetOrganizationStatusQueryHandlerTests()
    {
        var fakeOrgRepo = new FakeOrganizationRepository();
        _sut = new GetOrganizationStatusQueryHandler(fakeOrgRepo);

        _normalOrg = Any.Organization(tin: Tin.Create("12345678"));
        _trialOrg = Any.TrialOrganization(tin: Tin.Create("87654321"));

        _deactivatedOrg = Any.Organization(tin: Tin.Create("69696969"));
        _deactivatedOrg.Deactivate();

        fakeOrgRepo.AddAsync(_trialOrg, CancellationToken.None).Wait();
        fakeOrgRepo.AddAsync(_normalOrg, CancellationToken.None).Wait();
        fakeOrgRepo.AddAsync(_deactivatedOrg, CancellationToken.None).Wait();
    }

    [Fact]
    public async Task OrganizationDoesNotExist_LoginTypeIsTrial_ReturnsTrue()
    {
        var query = new GetOrganizationStatusQuery(Tin.Create("00000000").Value, LoginType: "trial");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task OrganizationDoesNotExist_LoginTypeIsNormal_ReturnsTrue()
    {
        var query = new GetOrganizationStatusQuery(Tin.Create("00000000").Value, LoginType: "normal");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task OrgStatusIsNormal_LoginTypeIsNormal_ReturnsTrue()
    {
        var query = new GetOrganizationStatusQuery(_normalOrg.Tin!.Value, LoginType: "normal");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task OrgStatusIsTrial_LoginTypeIsTrial_ReturnsTrue()
    {
        var query = new GetOrganizationStatusQuery(_trialOrg.Tin!.Value, LoginType: "trial");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task OrgStatusIsTrial_LoginTypeIsNormal_ReturnsFalse()
    {
        var query = new GetOrganizationStatusQuery(_trialOrg.Tin!.Value, LoginType: "normal");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task OrgStatusIsNormal_LoginTypeIsTrial_ReturnsFalse()
    {
        var query = new GetOrganizationStatusQuery(_normalOrg.Tin!.Value, LoginType: "trial");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task OrgStatusIsDeactivated_LoginTypeIsTrial_ReturnsFalse()
    {
        var query = new GetOrganizationStatusQuery(_deactivatedOrg.Tin!.Value, LoginType: "trial");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task OrgStatusIsDeactivated_LoginTypeIsNormal_ReturnsFalse()
    {
        var query = new GetOrganizationStatusQuery(_deactivatedOrg.Tin!.Value, LoginType: "normal");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task OrgStatusIsTrial_LoginTypeIsMixedCase_ReturnsTrue()
    {
        var query = new GetOrganizationStatusQuery(_trialOrg.Tin!.Value, LoginType: "TrIaL");
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task QueryWithEmptyTinString_LoginTypeIsTrial_ReturnsArgumentException()
    {
        var query = new GetOrganizationStatusQuery(Tin.Empty().Value, LoginType: "trial");
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task QueryWithEmptyTinString_LoginTypeIsNormal_ReturnsArgumentException()
    {
        var query = new GetOrganizationStatusQuery(Tin.Empty().Value, LoginType: "normal");
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.Handle(query, CancellationToken.None));
    }

    [Theory]
    // Normal Organization
    [InlineData("12345678", "vip", false)]
    [InlineData("12345678", "", false)]
    [InlineData("12345678", " ", false)]
    [InlineData("12345678", "\t", false)]
    [InlineData("12345678", "\n", false)]

    // Trial organization
    [InlineData("87654321", "", false)]
    [InlineData("87654321", " ", false)]
    [InlineData("87654321", "\t", false)]
    [InlineData("87654321", "\n", false)]

    // Deactivated organization
    [InlineData("69696969", "", false)]
    [InlineData("69696969", " ", false)]
    [InlineData("69696969", "\t", false)]
    [InlineData("69696969", "\n", false)]
    public async Task InvalidLoginTypes_ReturnExpected(string tin, string loginType, bool expected)
    {
        var query = new GetOrganizationStatusQuery(tin, loginType);
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.Equal(expected, result);
    }
}
