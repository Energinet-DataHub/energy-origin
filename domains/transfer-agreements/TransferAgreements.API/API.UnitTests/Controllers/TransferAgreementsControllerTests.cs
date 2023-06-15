using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Controllers;
using API.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTests
{
    private TransferAgreementsController controller;
    private readonly Mock<ITransferAgreementRepository> mockTransferAgreementRepository;

    private const string subject = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
    private const string atr = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private const string tin = "11223344";
    private const string cpn = "Company A/S";

    public TransferAgreementsControllerTests()
    {
        mockTransferAgreementRepository = new Mock<ITransferAgreementRepository>();
        mockTransferAgreementRepository
            .Setup(o => o.AddTransferAgreementToDb(It.IsAny<TransferAgreement>()))
            .ReturnsAsync((TransferAgreement transferAgreement) => transferAgreement);
    }

    private TransferAgreementsController CreateControllerWithMockedUser()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new("sub", subject),
            new("atr", atr),
            new("cpn", cpn),
            new("tin", tin)
        }, "mock"));

        return new TransferAgreementsController(mockTransferAgreementRepository.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

    }

    [Fact]
    public async Task Create_ShouldCallRepositoryOnce()
    {
        var request = new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");

        controller = CreateControllerWithMockedUser();
        await controller.Create(request);

        mockTransferAgreementRepository.Verify(repository => repository.AddTransferAgreementToDb(It.Is<TransferAgreement>(agreement =>
            agreement.SenderId == Guid.Parse(subject) &&
            agreement.ActorId == atr &&
            agreement.StartDate == DateTimeOffset.FromUnixTimeSeconds(request.StartDate) &&
            agreement.EndDate == DateTimeOffset.FromUnixTimeSeconds(request.EndDate) &&
            agreement.SenderName == cpn &&
            agreement.SenderTin == tin &&
            agreement.ReceiverTin == request.ReceiverTin
        )), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldCallRepositoryOnce()
    {
        controller = CreateControllerWithMockedUser();
        var id = Guid.NewGuid();
        await controller.Get(id);

        mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreement(id, subject, tin), Times.Once);
    }

    [Fact]
    public async Task List_ShouldCallRepositoryOnce()
    {
        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreementsList(Guid.Parse(subject), tin))
            .ReturnsAsync(new List<TransferAgreement>());

        controller = CreateControllerWithMockedUser();

        await controller.GetTransferAgreements();

        mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreementsList(Guid.Parse(subject), tin), Times.Once);
    }

    [Fact]
    public async Task GetTransferAgreementsList_ShouldReturnCorrectNumberOfAgreements()
    {
        var transferAgreements = new List<TransferAgreement>()
        {
            new()
            {
                Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1), SenderName = "Producent A/S",
                SenderTin = "32132112", ReceiverTin = "11223344"
            },
            new()
            {
                Id = Guid.NewGuid(), StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1), SenderName = "Zeroes A/S",
                SenderTin = "13371337", ReceiverTin = "10010010"
            },
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreementsList(Guid.Parse(subject), tin))
            .ReturnsAsync(transferAgreements);

        controller = CreateControllerWithMockedUser();

        var result = await controller.GetTransferAgreements();

        var okResult = result.Result as OkObjectResult;
        var agreements = (TransferAgreementsResponse) okResult.Value;

        agreements.Result.Count.Should().Be(transferAgreements.Count);
        mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreementsList(Guid.Parse(subject), tin), Times.Once);
    }
}
