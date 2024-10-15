using API.Authorization._Features_;
using API.Models;
using API.UnitTests.Repository;
using API.ValueObjects;
using FluentAssertions;
/*
namespace API.UnitTests._Queries_;

public class GetClientConsentsQueryTests
{
    readonly FakeOrganizationRepository _fakeOrganizationRepository = new();

    [Fact]
    async Task GivenClientId_WhenGettingConsent_ConsentReturned()
    {
        // Arrange
        var clientId = new IdpClientId(new Guid("b8a1eab0-6c43-4e66-8104-c805082c4bf7"));
        var client = Client.Create(clientId, new("Loz"), ClientType.Internal, "https://localhost:5001");

        var organization = Any.Organization();
        Consent.Create(organization, client, DateTimeOffset.UtcNow);
        await _fakeOrganizationRepository.AddAsync(organization, CancellationToken.None);

        // Act
        var handler = new GetClientGrantedConsentsQueryHandler(_fakeOrganizationRepository);
        var result = await handler.Handle(new GetClientGrantedConsentsQuery(client.IdpClientId), CancellationToken.None);

        // Assert
        result.GetClientConsentsQueryResultItems[0].OrganizationName.Should().Be(organization.Name);
        result.GetClientConsentsQueryResultItems[0].Tin.Should().Be(organization.Tin);
    }
} TODO
*/
