using API.Authorization._Features_.Internal;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class GetServiceProviderTermsForOrganizationQueryTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly GetServiceProviderTermsQueryHandler _handler;

    public GetServiceProviderTermsForOrganizationQueryTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _handler = new GetServiceProviderTermsQueryHandler(_organizationRepository);
    }

    [Fact]
    public async Task Handle_WhenOrganizationHasAcceptedTerms_ReturnsTrue()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        organization.AcceptServiceProviderTerms();
        await _organizationRepository.AddAsync(organization, CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOrganizationHasNotAcceptedTerms_ReturnsFalse()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_ThrowsEntityNotFoundException()
    {
        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(Guid.NewGuid()));

        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }
}
