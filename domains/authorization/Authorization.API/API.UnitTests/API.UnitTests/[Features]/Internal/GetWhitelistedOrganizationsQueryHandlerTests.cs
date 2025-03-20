using API.Authorization._Features_.Internal;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests._Features_.Internal;

public class GetWhitelistedOrganizationsQueryHandlerTests
{
    private readonly FakeWhitelistedRepository _fakeWhitelistedRepository;
    private readonly GetWhitelistedOrganizationsQueryHandler _handler;

    public GetWhitelistedOrganizationsQueryHandlerTests()
    {
        _fakeWhitelistedRepository = new FakeWhitelistedRepository();
        _handler = new GetWhitelistedOrganizationsQueryHandler(_fakeWhitelistedRepository);
    }

    [Fact]
    public async Task GivenDatabaseContainsWhitelistedRecords_WhenQuerying_ThenReturnListOfAllRecords()
    {
        var query = new GetWhitelistedOrganizationsQuery();

        var whitelisted1 = Any.Whitelisted(Tin.Create("12345678"));
        var whitelisted2 = Any.Whitelisted(Tin.Create("87654321"));

        await _fakeWhitelistedRepository.AddAsync(whitelisted1, CancellationToken.None);
        await _fakeWhitelistedRepository.AddAsync(whitelisted2, CancellationToken.None);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(new List<GetWhitelistedOrganizationsQueryResultItem>
        {
            new(whitelisted1.Id, whitelisted1.Tin.Value),
            new(whitelisted2.Id, whitelisted2.Tin.Value)
        }, result.Result);
    }

    [Fact]
    public async Task GivenNoWhitelistedRecords_WhenQuerying_ThenReturnEmptyList()
    {
        var query = new GetWhitelistedOrganizationsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Result);
    }
}
