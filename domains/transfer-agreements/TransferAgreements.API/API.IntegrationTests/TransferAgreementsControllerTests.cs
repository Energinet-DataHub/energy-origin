using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.ApiModels;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace API.IntegrationTests {

    public class TransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
    {
        private readonly TransferAgreementsApiWebApplicationFactory factory;

        public TransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
        {
            // Arrange
            var client = factory.CreateClient();
            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = 1662016400, // example Unix epoch timestamp
                EndDate = 1662102800, // example Unix epoch timestamp
                Tin = 12345678 // example 8-digit number
            };

            // Act
            var response = await client.PostAsJsonAsync("api/transfer-agreement", transferAgreement);
            response.EnsureSuccessStatusCode();
            var createdTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreement>();

            // Assert
            createdTransferAgreement.Should().NotBeNull();
            createdTransferAgreement.Should().BeEquivalentTo(transferAgreement, options => options.ExcludingMissingMembers());
        }
    }
}
