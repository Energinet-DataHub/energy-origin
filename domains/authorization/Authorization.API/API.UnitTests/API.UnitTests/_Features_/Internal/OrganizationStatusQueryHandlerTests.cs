using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class GetOrganizationStatusQueryHandlerTests
{
    private readonly GetOrganizationStatusQueryHandler _sut;

    public GetOrganizationStatusQueryHandlerTests()
    {
        var fakeOrgRepo = new FakeOrganizationRepository();
        _sut = new GetOrganizationStatusQueryHandler(fakeOrgRepo);

        Dictionary<string, Organization> orgMap = new()
        {
            ["12345678"] = Any.Organization(tin: Tin.Create("12345678")),
            ["87654321"] = Any.TrialOrganization(tin: Tin.Create("87654321")),
            ["69696969"] = Any.DeactivatedOrganization(tin: Tin.Create("69696969"))
        };

        foreach (var org in orgMap.Values)
            fakeOrgRepo.AddAsync(org, CancellationToken.None).Wait();
    }

    [Theory]
    [InlineData("00000000", "trial", true, OrganizationStatus.Trial)]
    [InlineData("00000000", "normal", true, OrganizationStatus.Normal)]
    [InlineData("12345678", "normal", true, OrganizationStatus.Normal)]
    [InlineData("87654321", "trial", true, OrganizationStatus.Trial)]
    [InlineData("87654321", "normal", false, OrganizationStatus.Trial)]
    [InlineData("12345678", "trial", false, OrganizationStatus.Normal)]
    [InlineData("69696969", "trial", false, OrganizationStatus.Deactivated)]
    [InlineData("69696969", "normal", true, OrganizationStatus.Deactivated)]
    [InlineData("87654321", "TrIaL", true, OrganizationStatus.Trial)]
    public async Task LoginTypeValidation_ReturnsExpectedResult(string tin, string loginType, bool expectedValid, OrganizationStatus? expectedStatus)
    {
        var query = new GetOrganizationStatusQuery(tin, loginType);
        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.Equal(expectedValid, result.IsAllowedAccess);
        Assert.Equal(expectedStatus, result.GrantedAccessAsTypeOf);
    }

    [Theory]
    [InlineData("", "trial")]
    [InlineData("", "normal")]
    public async Task EmptyTin_ThrowsArgumentException(string tin, string loginType)
    {
        var query = new GetOrganizationStatusQuery(tin, loginType);
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.Handle(query, CancellationToken.None));
    }

    [Theory]
    [InlineData("12345678", "vip")]
    [InlineData("12345678", "")]
    [InlineData("12345678", " ")]
    [InlineData("12345678", "\t")]
    [InlineData("12345678", "\n")]
    [InlineData("87654321", "")]
    [InlineData("87654321", " ")]
    [InlineData("87654321", "\t")]
    [InlineData("87654321", "\n")]
    [InlineData("69696969", "")]
    [InlineData("69696969", " ")]
    [InlineData("69696969", "\t")]
    [InlineData("69696969", "\n")]
    public async Task InvalidLoginTypes_ReturnExpected(string tin, string loginType)
    {
        var query = new GetOrganizationStatusQuery(tin, loginType);
        var result = await _sut.Handle(query, CancellationToken.None);
        Assert.False(result.IsAllowedAccess);
    }
}
