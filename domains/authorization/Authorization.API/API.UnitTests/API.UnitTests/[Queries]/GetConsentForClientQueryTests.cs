using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.Models;
using API.UnitTests.Repository;
using FluentAssertions;
/*
namespace API.UnitTests._Queries_;

public class GetConsentForClientQueryTests
{
    [Fact]
    public async Task GivenExistingClient_WhenQueryingConsent_ClientIsReturned()
    {
        var clientRepository = new FakeClientRepository();
        var client = Any.Client();
        await clientRepository.AddAsync(client, CancellationToken.None);
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var result = await sut.Handle(new GetConsentForClientQuery(client.IdpClientId.Value), CancellationToken.None);

        result.Should().NotBeNull();
        result.Scope.Should().Contain("dashboard");
        result.Scope.Should().Contain("production");
        result.Scope.Should().Contain("meters");
        result.Scope.Should().Contain("certificates");
        result.Scope.Should().Contain("wallet");
        result.Sub.Should().Be(client.IdpClientId.Value);
        result.OrgName.Should().Be(client.Name.Value);
        result.SubType.Should().Be("External");
        result.OrgIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenClientWithConsent_WhenQueryingConsent_OrgIdIsReturned()
    {
        var clientRepository = new FakeClientRepository();
        var client = Any.Client();
        var organization = Any.Organization();
        Consent.Create(organization, client, DateTimeOffset.Now);
        await clientRepository.AddAsync(client, CancellationToken.None);
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var result = await sut.Handle(new GetConsentForClientQuery(client.IdpClientId.Value), CancellationToken.None);

        result.Should().NotBeNull();
        result.OrgIds.Should().ContainSingle(id => id == organization.Id);
    }

    [Fact]
    public async Task GivenMultipleClients_WhenQueryingConsent_OnlyThisClientsConsentIsReturned()
    {
        var clientRepository = new FakeClientRepository();
        var otherClient = Any.Client();
        var client = Any.Client();
        var organization = Any.Organization();
        Consent.Create(organization, otherClient, DateTimeOffset.Now);
        await clientRepository.AddAsync(client, CancellationToken.None);
        await clientRepository.AddAsync(otherClient, CancellationToken.None);
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var result = await sut.Handle(new GetConsentForClientQuery(client.IdpClientId.Value), CancellationToken.None);

        result.Should().NotBeNull();
        result.OrgIds.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenQueryingConsent_ClientIsNotFound()
    {
        var clientRepository = new FakeClientRepository();
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var action = async () => await sut.Handle(new GetConsentForClientQuery(Any.Guid()), CancellationToken.None);
        await action.Should().ThrowAsync<EntityNotFoundException>();
    }
}
*/
