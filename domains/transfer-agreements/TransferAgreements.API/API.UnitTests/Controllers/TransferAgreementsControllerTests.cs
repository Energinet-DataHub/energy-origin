using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.Controllers;
using API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsController controller;
    private readonly Mock<ITransferAgreementService> mockTransferAgreementService;

    public TransferAgreementsControllerTests()
    {
        mockTransferAgreementService = new Mock<ITransferAgreementService>();
        controller = new TransferAgreementsController(mockTransferAgreementService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new("sub", "03bad0af-caeb-46e8-809c-1d35a5863bc7"),
            new("atr", "d4f32241-442c-4043-8795-a4e6bf574e7f")
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }


    [Fact]
    public async Task Create_ShouldCallServiceOnce()
    {
        var request = new CreateTransferAgreement
        {
            StartDate = DateTimeOffset.Now,
            EndDate = DateTimeOffset.Now.AddDays(1),
            ReceiverTin = "TestReceiverTin"
        };

        var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);
        var actorId = controller.ControllerContext.HttpContext.User.FindFirstValue("atr");

        await controller.Create(request);

        mockTransferAgreementService.Verify(service => service.CreateTransferAgreement(It.Is<TransferAgreement>(agreement =>
            agreement.SenderId == userId &&
            agreement.ActorId == actorId &&
            agreement.StartDate == request.StartDate.UtcDateTime &&
            agreement.EndDate == request.EndDate.UtcDateTime &&
            agreement.ReceiverTin == request.ReceiverTin
        )), Times.Once);
    }
}
