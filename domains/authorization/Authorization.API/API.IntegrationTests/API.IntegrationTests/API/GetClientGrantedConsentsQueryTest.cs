using System.Net.Http.Json;
using API.Authorization.Controllers;
using API.IntegrationTests.Setup;
using API.Models;
using API.UnitTests;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ClientType = API.Models.ClientType;

namespace API.IntegrationTests.API;

[Collection(IntegrationTestCollection.CollectionName)]
public class GetClientGrantedConsentsQueryTest : IntegrationTestBase
{
    private readonly Api _api;
    private readonly Guid _sub;

    public GetClientGrantedConsentsQueryTest(IntegrationTestFixture fixture) : base(fixture)
    {
        _sub = Guid.NewGuid();
        _api = fixture.WebAppFactory.CreateApi(_sub.ToString());
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_ThenClientConsentsReturned()
    {
        var client = Client.Create(new IdpClientId(_sub), new("Loz"), ClientType.Internal, "https://localhost:5001");
        var organization = Any.Organization();
        var organizationWithClient = Any.OrganizationWithClient(client: client);
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.Organizations.AddAsync(organizationWithClient);
        await _fixture.DbContext.OrganizationConsents.AddAsync(consent);
        await _fixture.DbContext.SaveChangesAsync();

        // When
        var response = await _api.GetClientConsents();
        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>();

        // Then
        response.Should().Be200Ok();
        result!.Result.Count().Should().Be(1);
        result!.Result.First().OrganizationName.Should().Be(organization.Name.Value);
        result!.Result.First().Tin.Should().Be(organization.Tin!.Value);
    }

    [Fact]
    public async Task GivenLoggedInClient_WhenGettingClientConsents_WithNoConsents_ThenEmptyResponseReturned()
    {
        var organizationWithClient = Any.OrganizationWithClient();
        var organization = Any.Organization();
        var consent = OrganizationConsent.Create(organization.Id, organizationWithClient.Id, DateTimeOffset.UtcNow);

        await _fixture.DbContext.Organizations.AddAsync(organizationWithClient);
        await _fixture.DbContext.Organizations.AddAsync(organization);
        await _fixture.DbContext.OrganizationConsents.AddAsync(consent);
        await _fixture.DbContext.SaveChangesAsync();

        // When
        var response = await _api.GetClientConsents();
        var result = await response.Content.ReadFromJsonAsync<ClientConsentsResponse>();

        // Then
        response.Should().Be200Ok();
        result!.Result.Should().BeEmpty();
    }

}
