using API.Authorization._Features_.Internal;
using API.Authorization.Exceptions;
using API.Models;
using API.UnitTests.Repository;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Features_.Internal;

public class GetServiceProviderTermsForOrganizationQueryTests
{
    private readonly FakeOrganizationRepository _organizationRepository;
    private readonly FakeServiceProviderTermsRepository _serviceProviderTermsRepository;
    private readonly GetServiceProviderTermsQueryHandler _handler;

    public GetServiceProviderTermsForOrganizationQueryTests()
    {
        _organizationRepository = new FakeOrganizationRepository();
        _serviceProviderTermsRepository = new FakeServiceProviderTermsRepository();
        _handler = new GetServiceProviderTermsQueryHandler(_organizationRepository, _serviceProviderTermsRepository);
    }

    [Fact]
    public async Task Handle_WhenOrganizationHasAcceptedLatestTerms_ReturnsTrue()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        organization.AcceptServiceProviderTerms(ServiceProviderTerms.Create(1));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _serviceProviderTermsRepository.AddAsync(ServiceProviderTerms.Create(1), CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenOrganizationHasNotAcceptedLatestTerms_ReturnsFalse()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _serviceProviderTermsRepository.AddAsync(ServiceProviderTerms.Create(1), CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenOrganizationDoesNotExist_ThrowsInvalidConfigurationException()
    {
        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(Guid.NewGuid()));

        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNoServiceProviderTermsExist_ThrowsInvalidConfigurationException()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        Func<Task> action = async () => await _handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOrganizationHasOlderTermsVersion_ReturnsFalse()
    {
        var organization = Organization.Create(Tin.Create("12345678"), OrganizationName.Create("Test Org"));
        organization.AcceptServiceProviderTerms(ServiceProviderTerms.Create(1));
        await _organizationRepository.AddAsync(organization, CancellationToken.None);
        await _serviceProviderTermsRepository.AddAsync(ServiceProviderTerms.Create(2), CancellationToken.None);

        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(organization.Id));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeFalse();
    }
}
