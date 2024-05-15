using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;

namespace API.UnitTests._Queries_;

public class GetConsentForClientQueryTests
{


    [Fact]
    public async Task Return_External_Client_When_Everything_Is_Awesome()
    {
        // Arrange
        var clientRepository = new FakeClientRepository();
        var client = Any.Client();
        await clientRepository.AddAsync(client, CancellationToken.None);
        var sut = new GetConsentForClientQueryHandler(clientRepository);

        // Act
        var result = await sut.Handle(new GetConsentForClientQuery(client.IdpClientId.Value), CancellationToken.None);

        // Assert

        result.Should().NotBe(null);

    }
}
