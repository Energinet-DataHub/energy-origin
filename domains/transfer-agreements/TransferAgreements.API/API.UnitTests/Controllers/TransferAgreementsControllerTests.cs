using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.Controllers;
using API.Data;
using FluentAssertions.Equivalency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsController controller;
    private readonly Mock<ITransferAgreementRepository> mockTransferAgreementRepository;

    private readonly string subject = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
    private readonly string atr = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private readonly string tin = "11223344";
    public TransferAgreementsControllerTests()
    {
        mockTransferAgreementRepository = new Mock<ITransferAgreementRepository>();
        mockTransferAgreementRepository
            .Setup(o => o.AddTransferAgreementToDb(It.IsAny<TransferAgreement>()))
            .ReturnsAsync((TransferAgreement transferAgreement) => transferAgreement);

        controller = new TransferAgreementsController(mockTransferAgreementRepository.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new("sub", subject),
            new("atr", atr),
            new("tin", tin)
        }, "mock"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Create_ShouldCallRepositoryOnce()
    {
        var request = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");
        var userId = Guid.Parse(controller.ControllerContext.HttpContext.User.FindFirstValue("sub") ?? string.Empty);
        var actorId = controller.ControllerContext.HttpContext.User.FindFirstValue("atr");

        await controller.Create(request);

        mockTransferAgreementRepository.Verify(repository => repository.AddTransferAgreementToDb(It.Is<TransferAgreement>(agreement =>
            agreement.SenderId == userId &&
            agreement.ActorId == actorId &&
            agreement.StartDate == DateTimeOffset.FromUnixTimeSeconds(request.StartDate) &&
            agreement.EndDate == DateTimeOffset.FromUnixTimeSeconds(request.EndDate) &&
            agreement.ReceiverTin == request.ReceiverTin
        )), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldCallRepositoryOnce()
    {
        var id = Guid.NewGuid();
        await controller.Get(id);

        mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreement(id, subject, tin), Times.Once);
    }
}
