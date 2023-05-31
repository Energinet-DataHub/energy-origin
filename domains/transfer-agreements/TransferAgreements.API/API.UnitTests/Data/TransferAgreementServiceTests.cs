using System;
using System.Threading.Tasks;
using API.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.UnitTests.Data
{
    public class TransferAgreementServiceTests
    {
        [Fact]
        public async Task CreateTransferAgreement_ShouldAddTransferAgreementToDatabase()
        {

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var service = new TransferAgreementService(context);

            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.NewGuid(),
                ActorId = Guid.NewGuid().ToString(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                ReceiverTin = "12345678"
            };

            var result = await service.CreateTransferAgreement(transferAgreement);

            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();

            var addedTransferAgreement = await context.TransferAgreements.FindAsync(result.Id);
            addedTransferAgreement.Should().BeEquivalentTo(result);
        }
    }
}
