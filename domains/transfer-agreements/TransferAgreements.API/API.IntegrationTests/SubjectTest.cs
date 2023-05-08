using System;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Xunit;

namespace API.IntegrationTests;

/// <summary>
/// Delete this test. Is only an example of how to get sub UUID
/// </summary>
public class SubjectTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public SubjectTest(TransferAgreementsApiWebApplicationFactory factory)
        => this.factory = factory;

    [Fact]
    public async Task Something()
    {
        var sub = Guid.NewGuid().ToString();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub);

        var subjectResponse = await authenticatedClient.GetStringAsync("api/transfer-agreements/subject");

        subjectResponse.Should().Be(sub);
    }
}
