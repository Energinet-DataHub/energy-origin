using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class OrganizationStateQueryHandlerTests
{
    private readonly FakeOrganizationRepository _fakeOrganizationRepository;
    private readonly FakeTermsRepository _fakeTermsRepository;
    private readonly OrganizationStateQueryHandler _handler;

    public OrganizationStateQueryHandlerTests()
    {
        _fakeOrganizationRepository = new FakeOrganizationRepository();
        _fakeTermsRepository = new FakeTermsRepository();
        _handler = new OrganizationStateQueryHandler(_fakeOrganizationRepository, _fakeTermsRepository);
    }

    [Fact]
    public async Task GivenValidOrganization_WhenHandlingQuery_ThenReturnsTrue()
    {
        var tin = "12345678";
        var organization = Organization.Create(Tin.Create(tin), OrganizationName.Create("Test Org"));
        organization.AcceptTerms(new Terms("1.0"));
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        var latestTerms = new Terms("1.0");
        await _fakeTermsRepository.AddAsync(latestTerms, CancellationToken.None);

        var query = new OrganizationStateQuery(tin);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GivenNoOrganization_WhenHandlingQuery_ThenReturnsFalse()
    {
        var tin = "12345678";
        var latestTerms = new Terms("1.0");
        await _fakeTermsRepository.AddAsync(latestTerms, CancellationToken.None);

        var query = new OrganizationStateQuery(tin);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOrganizationWithNoTermsAccepted_WhenHandlingQuery_ThenReturnsFalse()
    {
        var tin = "12345678";
        var organization = Organization.Create(Tin.Create(tin), OrganizationName.Create("Test Org"));
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        var latestTerms = new Terms("1.0");
        await _fakeTermsRepository.AddAsync(latestTerms, CancellationToken.None);

        var query = new OrganizationStateQuery(tin);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOrganizationWithOutdatedTerms_WhenHandlingQuery_ThenReturnsFalse()
    {
        var tin = "12345678";
        var organization = Organization.Create(Tin.Create(tin), OrganizationName.Create("Test Org"));
        organization.AcceptTerms(new Terms("0.9"));
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        var latestTerms = new Terms("1.0");
        await _fakeTermsRepository.AddAsync(latestTerms, CancellationToken.None);

        var query = new OrganizationStateQuery(tin);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }
}
