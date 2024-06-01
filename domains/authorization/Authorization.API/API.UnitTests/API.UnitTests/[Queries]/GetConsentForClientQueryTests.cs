using API.Authorization._Features_;
using API.Authorization.Exceptions;
using API.UnitTests.Repository;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class GetConsentForClientQueryTests
{
    [Fact]
    public async Task GivenClientIdExist_WhenQueryingForAuthorization_ReturnsOKResult()
    {
        var clientRepository = new FakeClientRepository();
        var client = Any.Client();
        await clientRepository.AddAsync(client, CancellationToken.None);
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var result = await sut.Handle(new GetConsentForClientQuery(client.IdpClientId.Value), CancellationToken.None);

        result.Should().NotBe(null);
    }

    [Fact]
    public async Task GivenUnknownClientId_WhenQueryingForAuthorization_ClientIsNotFound()
    {
        var clientRepository = new FakeClientRepository();
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        var action = async () => await sut.Handle(new GetConsentForClientQuery(Any.Guid()), CancellationToken.None);
        await action.Should().ThrowAsync<EntityNotFoundException>();
    }
}
