using System;
using System.Threading.Tasks;
using API.ApiModels;
using API.ApiModels.Requests;
using API.Controllers;
using API.Data;
using FluentAssertions;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers
{
    public class TransferAgreementsControllerTests
    {
        private readonly Mock<ITransferAgreementService> mockTransferAgreementService;
        private readonly TransferAgreementsController controller;

        public TransferAgreementsControllerTests()
        {
            mockTransferAgreementService = new Mock<ITransferAgreementService>();
            controller = new TransferAgreementsController(mockTransferAgreementService.Object);
        }

        [Fact]
        public async Task Create_ShouldCallServiceCreateOnce()
        {
            var request = new CreateTransferAgreement();

            mockTransferAgreementService.Setup(service => service.CreateTransferAgreement(It.IsAny<TransferAgreement>()))
                .ReturnsAsync(new TransferAgreement());

            await controller.Create(request);

            mockTransferAgreementService.Verify(service => service.CreateTransferAgreement(It.IsAny<TransferAgreement>()), Times.AtMostOnce());
        }
    }
}
