using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Controllers;
using API.Data;
using API.Services;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsController controller;
    private readonly Mock<ITransferAgreementRepository> mockTransferAgreementRepository;
    private readonly Mock<IWalletDepositEndpointService> mockWalletDepositEndpointService;

    private const string subject = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
    private const string atr = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private const string tin = "11223344";
    private const string cpn = "Company A/S";

    public TransferAgreementsControllerTests()
    {
        mockTransferAgreementRepository = new Mock<ITransferAgreementRepository>();
        mockWalletDepositEndpointService = new Mock<IWalletDepositEndpointService>();
        Mock<IValidator<CreateTransferAgreement>> mockValidator = new();
        Mock<IHttpContextAccessor> mockHttpContextAccessor = new();

        mockTransferAgreementRepository
            .Setup(o => o.AddTransferAgreementToDb(It.IsAny<TransferAgreement>()))
            .ReturnsAsync((TransferAgreement transferAgreement) => transferAgreement);

        mockValidator
            .Setup(o => o.ValidateAsync(It.IsAny<CreateTransferAgreement>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mockContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new("sub", subject),
                new("atr", atr),
                new("cpn", cpn),
                new("tin", tin)
            }, "mock"))
        };

        mockContext.Request.Headers["Authorization"] = $"Bearer sample.jwt.token";

        mockHttpContextAccessor.Setup(x =>
            x.HttpContext).Returns(mockContext);

        controller = new TransferAgreementsController(
            mockTransferAgreementRepository.Object,
            mockValidator.Object,
            mockWalletDepositEndpointService.Object,
            mockHttpContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = mockHttpContextAccessor.Object.HttpContext! }
        };
    }

    [Fact]
    public async Task Create_ShouldCallRepositoryOnce()
    {
        var request = new CreateTransferAgreement(
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            "13371337",
            Base64EncodedWalletDepositEndpoint: Some.Base64EncodedWalletDepositEndpoint);

        await controller.Create(request);

        mockTransferAgreementRepository.Verify(repository =>
            repository.AddTransferAgreementToDb(It.Is<TransferAgreement>(agreement =>
            agreement.SenderId == Guid.Parse(subject) &&
            agreement.StartDate == DateTimeOffset.FromUnixTimeSeconds(request.StartDate) &&
            agreement.EndDate == DateTimeOffset.FromUnixTimeSeconds(request.EndDate!.Value) &&
            agreement.SenderName == cpn &&
            agreement.SenderTin == tin &&
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

    [Fact]
    public async Task List_ShouldCallRepositoryOnce()
    {
        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreementsList(Guid.Parse(subject), tin))
            .ReturnsAsync(new List<TransferAgreement>());

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

        var result = await controller.GetTransferAgreements();

        var okResult = result.Result as OkObjectResult;
        var agreements = okResult?.Value as TransferAgreementsResponse;

        agreements!.Result.Count.Should().Be(transferAgreements.Count);
        mockTransferAgreementRepository.Verify(repository => repository.GetTransferAgreementsList(Guid.Parse(subject), tin), Times.Once);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenTransferAgreementNotFoundOrUserIdNotMatched()
    {
        var differentUserId = Guid.NewGuid();
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = differentUserId,
            EndDate = DateTimeOffset.UtcNow.AddDays(10)
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin))
            .ReturnsAsync(transferAgreement);

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<NotFoundResult>();
        mockTransferAgreementRepository.Verify(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin), Times.Once);
        mockTransferAgreementRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(subject),
            EndDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin))
            .ReturnsAsync(transferAgreement);

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        mockTransferAgreementRepository.Verify(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin), Times.Once);
        mockTransferAgreementRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(subject),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = tin
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin))
            .ReturnsAsync(transferAgreement);
        mockTransferAgreementRepository
            .Setup(o => o.HasDateOverlap(It.IsAny<TransferAgreement>()))
            .ReturnsAsync(true);

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<ConflictObjectResult>();
        mockTransferAgreementRepository.Verify(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin), Times.Once);
        mockTransferAgreementRepository.Verify(o => o.HasDateOverlap(It.IsAny<TransferAgreement>()), Times.Once);
        mockTransferAgreementRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EditEndDate_ShouldUpdateTransferAgreement_WhenInputIsValid()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(subject),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = tin
        };

        mockTransferAgreementRepository
            .Setup(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin))
            .ReturnsAsync(transferAgreement);
        mockTransferAgreementRepository
            .Setup(o => o.HasDateOverlap(It.IsAny<TransferAgreement>()))
            .ReturnsAsync(false);

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(newEndDate));

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(newEndDate), TimeSpan.FromSeconds(1));
        mockTransferAgreementRepository.Verify(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin), Times.Once);
        mockTransferAgreementRepository.Verify(o => o.HasDateOverlap(It.IsAny<TransferAgreement>()), Times.Once);
        mockTransferAgreementRepository.Verify(o => o.Save(), Times.Once);
    }

    [Fact]
    public async Task EditEndDate_ShouldUpdateTransferAgreement_WhenInputIsValidAndEndDateIsNull()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(subject),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = tin
        };

        mockTransferAgreementRepository
            .Setup(o =>
                o.GetTransferAgreement(transferAgreement.Id,
                    subject,
                    tin))
            .ReturnsAsync(transferAgreement);

        mockTransferAgreementRepository
            .Setup(o =>
                o.HasDateOverlap(It.IsAny<TransferAgreement>()))
            .ReturnsAsync(false);

        var newEndDate = new EditTransferAgreementEndDate(null);

        var result = await controller.EditEndDate(transferAgreement.Id, newEndDate);

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeNull();
        mockTransferAgreementRepository.Verify(o => o.GetTransferAgreement(transferAgreement.Id, subject, tin), Times.Once);
        mockTransferAgreementRepository.Verify(o => o.HasDateOverlap(It.IsAny<TransferAgreement>()), Times.Once);
        mockTransferAgreementRepository.Verify(o => o.Save(), Times.Once);
    }

    [Fact]
    public async Task CreateWalletDepositEndpoint_ShouldPassTokenWithoutBearerPrefix()
    {
        const string expectedJwtToken = "Bearer sample.jwt.token";

        string passedToken = null!;
        mockWalletDepositEndpointService
            .Setup(s =>
                s.CreateWalletDepositEndpoint(It.IsAny<string>()))
            .Callback<string>(token => passedToken = token)
            .ReturnsAsync("sampleBase64String");

        await controller.CreateWalletDepositEndpoint();

        passedToken.Should().Be(expectedJwtToken);
    }
}
