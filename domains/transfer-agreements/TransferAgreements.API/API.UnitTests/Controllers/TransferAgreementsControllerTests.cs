using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsController controller;
    private readonly ITransferAgreementRepository mockTransferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
    private readonly IProjectOriginWalletService mockProjectOriginWalletDepositEndpointService = Substitute.For<IProjectOriginWalletService>();

    private const string subject = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
    private const string atr = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private const string tin = "11223344";
    private const string cpn = "Company A/S";

    public TransferAgreementsControllerTests()
    {
        var mockValidator = Substitute.For<IValidator<CreateTransferAgreement>>();
        var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        mockTransferAgreementRepository.AddTransferAgreementToDb(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(new TransferAgreement()));

        mockValidator.ValidateAsync(Arg.Any<CreateTransferAgreement>())
            .Returns(Task.FromResult(new FluentValidation.Results.ValidationResult()));

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

        mockHttpContextAccessor.HttpContext.Returns(mockContext);

        controller = new TransferAgreementsController(
            mockTransferAgreementRepository,
            mockValidator,
            mockProjectOriginWalletDepositEndpointService,
            mockHttpContextAccessor)
        {
            ControllerContext = new ControllerContext { HttpContext = mockHttpContextAccessor.HttpContext! }
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

        await mockTransferAgreementRepository.Received(1).AddTransferAgreementToDb(Arg.Any<TransferAgreement>());
    }

    [Fact]
    public async Task Get_ShouldCallRepositoryOnce()
    {
        var id = Guid.NewGuid();
        await controller.Get(id);

        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(id, subject, tin);
    }

    [Fact]
    public async Task List_ShouldCallRepositoryOnce()
    {
        mockTransferAgreementRepository
            .GetTransferAgreementsList(Guid.Parse(subject), tin).Returns(Task.FromResult(new List<TransferAgreement>()));

        await controller.GetTransferAgreements();

        await mockTransferAgreementRepository.Received(1).GetTransferAgreementsList(Guid.Parse(subject), tin);
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

        mockTransferAgreementRepository.GetTransferAgreementsList(Guid.Parse(subject), tin).Returns(Task.FromResult(transferAgreements));

        var result = await controller.GetTransferAgreements();

        var okResult = result.Result as OkObjectResult;
        var agreements = okResult?.Value as TransferAgreementsResponse;

        agreements!.Result.Count.Should().Be(transferAgreements.Count);
        await mockTransferAgreementRepository.Received(1).GetTransferAgreementsList(Guid.Parse(subject), tin);
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, subject, tin).Returns(Task.FromResult(transferAgreement));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<NotFoundResult>();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, subject, tin);
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, subject, tin).Returns(Task.FromResult(transferAgreement));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, subject, tin);
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, subject, tin).Returns(Task.FromResult(transferAgreement));

        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(true));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<ConflictObjectResult>();

        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, subject, tin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, subject, tin).Returns(Task.FromResult(transferAgreement));

        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(false));


        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(newEndDate));

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(newEndDate), TimeSpan.FromSeconds(1));
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, subject, tin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
        await mockTransferAgreementRepository.Received(1).Save();
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, subject, tin).Returns(Task.FromResult(transferAgreement));


        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(false));


        var newEndDate = new EditTransferAgreementEndDate(null);

        var result = await controller.EditEndDate(transferAgreement.Id, newEndDate);

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeNull();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, subject, tin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
        await mockTransferAgreementRepository.Received(1).Save();
    }

    [Fact]
    public async Task CreateWalletDepositEndpoint_ShouldPassTokenWithoutBearerPrefix()
    {
        const string expectedJwtToken = "Bearer sample.jwt.token";

        string passedToken = null!;
        mockProjectOriginWalletDepositEndpointService
            .When(x => x.CreateWalletDepositEndpoint(Arg.Any<string>()))
            .Do(x => passedToken = x.Arg<string>());

        await controller.CreateWalletDepositEndpoint();

        passedToken.Should().Be(expectedJwtToken);
    }
}
