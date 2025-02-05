using API.Authorization._Features_.Internal;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class GetOrganizationsQueryTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly GetOrganizationsQueryHandler _handler;

    public GetOrganizationsQueryTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _handler = new GetOrganizationsQueryHandler(_fakeOrganizationRepository);
    }

    [Fact]
    public async Task GivenDatabaseContainsOrganizations_WhenQuerying_ReturnsListOfAllOrganizations()
    {
        var query = new GetOrganizationsQueryRequest();

        var organization1 = Any.Organization(null, OrganizationName.Create("Brian Bolighaj A/S"));
        var organization2 = Any.Organization(null, OrganizationName.Create("Bolig Brianhaj S/A"));

        await _fakeOrganizationRepository.AddAsync(organization1, CancellationToken.None);
        await _fakeOrganizationRepository.AddAsync(organization2, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Result.Should().BeEquivalentTo(new List<GetOrganizationsQueryResult>
        {
            new(organization1.Id, organization1.Name.Value, organization1.Tin!.Value),
            new(organization2.Id, organization2.Name.Value, organization2.Tin!.Value)
        });
    }

    [Fact]
    public async Task GivenNoOrganizations_WhenQuerying_ReturnsEmptyList()
    {
        var query = new GetOrganizationsQueryRequest();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Result.Should().BeEmpty();
    }
}
