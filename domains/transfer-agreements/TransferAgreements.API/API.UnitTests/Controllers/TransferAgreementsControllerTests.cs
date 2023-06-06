using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
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
    private readonly Mock<ITransferAgreementRepository> mockTransferAgreementRepository;

    public TransferAgreementsControllerTests()
    {
        mockTransferAgreementRepository = new Mock<ITransferAgreementRepository>();
        mockTransferAgreementRepository
            .Setup(o => o.AddTransferAgreementToDb(It.IsAny<TransferAgreement>()))
            .ReturnsAsync((TransferAgreement transferAgreement) => transferAgreement);

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreementsBySubjectId(It.IsAny<Guid>()))
            .ReturnsAsync(new List<TransferAgreementResponse>());

        controller = new TransferAgreementsController(mockTransferAgreementRepository.Object);

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
        var request = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");
        var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);
        var actorId = controller.ControllerContext.HttpContext.User.FindFirstValue("atr");

        await controller.Create(request);

        mockTransferAgreementRepository.Verify(service => service.AddTransferAgreementToDb(It.Is<TransferAgreement>(agreement =>
            agreement.SenderId == userId &&
            agreement.ActorId == actorId &&
            agreement.StartDate == DateTimeOffset.FromUnixTimeSeconds(request.StartDate) &&
            agreement.EndDate == DateTimeOffset.FromUnixTimeSeconds(request.EndDate) &&
            agreement.ReceiverTin == request.ReceiverTin
        )), Times.Once);
    }

    [Fact]
    public async Task GetBySubjectId_ShouldCallServiceOnce()
    {
        var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);

        await controller.GetBySubjectId();

        mockTransferAgreementRepository.Verify(service => service.GetTransferAgreementsBySubjectId(userId), Times.Once);
    }

    [Fact]
    public async Task GetBySubjectId_ShouldReturnCorrectNumberOfAgreements()
    {
        var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);
        var transferAgreements = new List<TransferAgreementResponse>()
        {
            new(Guid.NewGuid(), 1633024867, 1633025867, "1234567890"),
            new(Guid.NewGuid(), 1633025868, 1633026868, "0987654321")
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreementsBySubjectId(userId))
            .ReturnsAsync(transferAgreements);

        var result = await controller.GetBySubjectId();

        var okResult = result.Result as OkObjectResult;
        var agreements = okResult.Value as List<TransferAgreementResponse>;

        Assert.Equal(transferAgreements.Count, agreements.Count);
    }

}
