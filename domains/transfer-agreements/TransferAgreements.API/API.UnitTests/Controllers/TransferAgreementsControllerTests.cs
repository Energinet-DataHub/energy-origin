using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.Controllers;
using API.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", "03bad0af-caeb-46e8-809c-1d35a5863bc7"),
                new Claim("atr", "d4f32241-442c-4043-8795-a4e6bf574e7f")
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task Create_ShouldCallServiceOnce()
        {

            var request = new CreateTransferAgreement();

            mockTransferAgreementService.Setup(service => service.CreateTransferAgreement(It.IsAny<TransferAgreement>()))
                .ReturnsAsync(new TransferAgreement());


            await controller.Create(request);


            mockTransferAgreementService.Verify(service => service.CreateTransferAgreement(It.IsAny<TransferAgreement>()), Times.AtMostOnce);
        }
    }
}
