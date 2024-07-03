using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class GetLatestTermsQueryHandlerTests
{
    private readonly FakeTermsRepository _fakeTermsRepository;
    private readonly GetLatestTermsQueryHandler _handler;

    public GetLatestTermsQueryHandlerTests()
    {
        _fakeTermsRepository = new FakeTermsRepository();
        _handler = new GetLatestTermsQueryHandler(_fakeTermsRepository);
    }

    [Fact]
    public async Task GivenMultipleTerms_WhenHandlingQuery_ThenReturnsLatestTerms()
    {
        var olderTerms = new Terms("0.9") { EffectiveDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var latestTerms = new Terms("1.0") { EffectiveDate = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero) };

        await _fakeTermsRepository.AddAsync(olderTerms, CancellationToken.None);
        await _fakeTermsRepository.AddAsync(latestTerms, CancellationToken.None);

        var query = new GetLatestTermsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().Be(latestTerms);
    }

    [Fact]
    public async Task GivenNoTerms_WhenHandlingQuery_ThenReturnsNull()
    {
        var query = new GetLatestTermsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }
}
