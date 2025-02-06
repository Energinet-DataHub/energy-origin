using API.Authorization._Features_.Internal;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class GetFirstPartyOrganizationsQueryTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly GetFirstPartyOrganizationsQueryHandler _handler;

    public GetFirstPartyOrganizationsQueryTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _handler = new GetFirstPartyOrganizationsQueryHandler(_fakeOrganizationRepository);
    }

    [Fact]
    public async Task GivenDatabaseContainsOrganizations_WhenQuerying_ReturnsListOfAllOrganizations()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var organization1 = Any.Organization(Tin.Create("12345678"), OrganizationName.Create("Brian Bolighaj A/S"));
        var organization2 = Any.Organization(Tin.Create("87654321"), OrganizationName.Create("Bolig Brianhaj S/A"));

        await _fakeOrganizationRepository.AddAsync(organization1, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(organization2, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Result.Should().BeEquivalentTo(new List<GetFirstPartyOrganizationsQueryResultItem>
        {
            new(organization1.Id, organization1.Name.Value, organization1.Tin!.Value),
            new(organization2.Id, organization2.Name.Value, organization2.Tin!.Value)
        });
    }

    [Fact]
    public async Task GivenDatabaseContainsMultipleOrganizations_But_OnlyASingleOneWithATin_WhenQuerying_ThenReturnAListWithTinLessOrganizations()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var firstPartyOrganization = Any.Organization(Tin.Create("12345678"), OrganizationName.Create("Brian Bolighaj A/S"));
        var thirdPartyOrganization = Any.OrganizationWithClient(Tin.Empty());

        await _fakeOrganizationRepository.AddAsync(firstPartyOrganization, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(thirdPartyOrganization, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Result.Should().BeEquivalentTo(new List<GetFirstPartyOrganizationsQueryResultItem>
        {
            new(firstPartyOrganization.Id, firstPartyOrganization.Name.Value, firstPartyOrganization.Tin!.Value)
        });
    }

    [Fact]
    public async Task GivenNoOrganizations_WhenQuerying_ReturnsEmptyList()
    {
        var query = new GetFirstPartyOrganizationsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Result.Should().BeEmpty();
    }
}
