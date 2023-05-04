using System;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

public class SomeTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public SomeTest(TransferAgreementsApiWebApplicationFactory factory)
        => this.factory = factory;

    [Fact]
    public async Task Something()
    {
        var authenticatedClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var subjectResponse = await authenticatedClient.GetStringAsync("api/transfer-agreements/subject");

        //subjectResponse.Should().Be("foo");
        subjectResponse.Should().NotBeEmpty();
    }
}
