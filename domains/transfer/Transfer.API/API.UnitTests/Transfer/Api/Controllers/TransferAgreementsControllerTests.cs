using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Transfer.Api.Models;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using API.Transfer.Api.v2023_01_01.Controllers;
using API.Transfer.Api.v2023_01_01.Dto.Requests;
using API.Transfer.Api.v2023_01_01.Dto.Responses;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.Api.Controllers;

public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsController controller;
    private readonly ITransferAgreementRepository mockTransferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
    private readonly ITransferAgreementProposalRepository mockTransferAgreementProposalRepository = Substitute.For<ITransferAgreementProposalRepository>();
    private readonly IProjectOriginWalletService mockProjectOriginWalletDepositEndpointService = Substitute.For<IProjectOriginWalletService>();

    private const string UserClaimNameScope = "userScope";
    private const string UserClaimNameActorLegacy = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private const string UserClaimNameActor = "d4f32241-442c-4043-8795-a4e6bf574e7f";
    private const string UserClaimNameTin = "11223344";
    private const string UserClaimNameOrganizationName = "Company A/S";
    private const string JwtRegisteredClaimNamesName = "Charlie Company";
    private const int UserClaimNameProviderType = 3;
    private const string UserClaimNameAllowCprLookup = "false";
    private const string UserClaimNameAccessToken = "";
    private const string UserClaimNameIdentityToken = "";
    private const string UserClaimNameProviderKeys = "";
    private const string UserClaimNameOrganizationId = "03bad0af-caeb-46e8-809c-1d35a5863bc7";
    private const string UserClaimNameMatchedRoles = "";
    private const string UserClaimNameRoles = "";
    private const string UserClaimNameAssignedRoles = "";

    public TransferAgreementsControllerTests()
    {
        var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        mockTransferAgreementRepository.AddTransferAgreementToDb(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(new TransferAgreement()));

        var mockContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new(UserClaimName.Scope, UserClaimNameScope),
                new(UserClaimName.ActorLegacy, UserClaimNameActorLegacy),
                new(UserClaimName.Actor, UserClaimNameActor),
                new(UserClaimName.Tin, UserClaimNameTin),
                new(UserClaimName.OrganizationName, UserClaimNameOrganizationName),
                new(JwtRegisteredClaimNames.Name, JwtRegisteredClaimNamesName),
                new(UserClaimName.ProviderType, UserClaimNameProviderType.ToString()),
                new(UserClaimName.AllowCprLookup, UserClaimNameAllowCprLookup),
                new(UserClaimName.AccessToken, UserClaimNameAccessToken),
                new(UserClaimName.IdentityToken, UserClaimNameIdentityToken),
                new(UserClaimName.ProviderKeys, UserClaimNameProviderKeys),
                new(UserClaimName.OrganizationId, UserClaimNameOrganizationId),
                new(UserClaimName.MatchedRoles, UserClaimNameMatchedRoles),
                new(UserClaimName.Roles, UserClaimNameRoles),
                new(UserClaimName.AssignedRoles, UserClaimNameAssignedRoles)
            }, "mock"))
        };

        mockContext.Request.Headers["Authorization"] = $"Bearer sample.jwt.token";

        mockHttpContextAccessor.HttpContext.Returns(mockContext);

        controller = new TransferAgreementsController(
            mockTransferAgreementRepository,
            mockProjectOriginWalletDepositEndpointService,
            mockHttpContextAccessor,
            mockTransferAgreementProposalRepository)
        {
            ControllerContext = new ControllerContext { HttpContext = mockHttpContextAccessor.HttpContext! }
        };
    }

    [Fact]
    public async Task Create_ShouldCallRepositoryOnce()
    {
        var taProposal = new TransferAgreementProposal
        {
            ReceiverCompanyTin = UserClaimNameTin,
            CreatedAt = DateTimeOffset.UtcNow,
            SenderCompanyId = Guid.NewGuid(),
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            SenderCompanyName = "SomeCompany",
            SenderCompanyTin = "32132132"
        };
        mockTransferAgreementProposalRepository.GetNonExpiredTransferAgreementProposalAsNoTracking(Arg.Any<Guid>())
            .Returns(taProposal);
        mockTransferAgreementRepository.AddTransferAgreementAndDeleteProposal(Arg.Any<TransferAgreement>(), taProposal.Id)
            .Returns(new TransferAgreement
            {
                EndDate = taProposal.EndDate,
                Id = Guid.NewGuid(),
                ReceiverReference = Guid.NewGuid(),
                ReceiverTin = UserClaimNameTin,
                SenderId = taProposal.SenderCompanyId,
                SenderName = taProposal.SenderCompanyName,
                SenderTin = taProposal.SenderCompanyTin,
                StartDate = taProposal.StartDate,
                TransferAgreementNumber = 1
            });

        var request = new CreateTransferAgreement(taProposal.Id);

        await controller.Create(request);

        await mockTransferAgreementRepository.Received(1).AddTransferAgreementAndDeleteProposal(Arg.Any<TransferAgreement>(), taProposal.Id);
    }

    [Fact]
    public async Task Get_ShouldCallRepositoryOnce()
    {
        var id = Guid.NewGuid();
        await controller.Get(id);

        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(id, UserClaimNameOrganizationId, UserClaimNameTin);
    }

    [Fact]
    public async Task List_ShouldCallRepositoryOnce()
    {
        mockTransferAgreementRepository
            .GetTransferAgreementsList(Guid.Parse(UserClaimNameOrganizationId), UserClaimNameTin).Returns(Task.FromResult(new List<TransferAgreement>()));

        await controller.GetTransferAgreements();

        await mockTransferAgreementRepository.Received(1).GetTransferAgreementsList(Guid.Parse(UserClaimNameOrganizationId), UserClaimNameTin);
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

        mockTransferAgreementRepository.GetTransferAgreementsList(Guid.Parse(UserClaimNameOrganizationId), UserClaimNameTin).Returns(Task.FromResult(transferAgreements));

        var result = await controller.GetTransferAgreements();

        var okResult = result.Result as OkObjectResult;
        var agreements = okResult?.Value as TransferAgreementsResponse;

        agreements!.Result.Count.Should().Be(transferAgreements.Count);
        await mockTransferAgreementRepository.Received(1).GetTransferAgreementsList(Guid.Parse(UserClaimNameOrganizationId), UserClaimNameTin);
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

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin)!.Returns(Task.FromResult(transferAgreement));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<NotFoundResult>();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(UserClaimNameOrganizationId),
            EndDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin)!.Returns(Task.FromResult(transferAgreement));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(UserClaimNameOrganizationId),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = UserClaimNameTin
        };

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin)!.Returns(Task.FromResult(transferAgreement));

        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(true));

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds()));

        result.Result.Should().BeOfType<ConflictObjectResult>();

        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
    }

    [Fact]
    public async Task EditEndDate_ShouldUpdateTransferAgreement_WhenInputIsValid()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(UserClaimNameOrganizationId),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = UserClaimNameTin
        };

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin)!.Returns(Task.FromResult(transferAgreement));

        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(false));


        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();

        var result = await controller.EditEndDate(transferAgreement.Id, new EditTransferAgreementEndDate(newEndDate));

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeCloseTo(DateTimeOffset.FromUnixTimeSeconds(newEndDate), TimeSpan.FromSeconds(1));
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
        await mockTransferAgreementRepository.Received(1).Save();
    }

    [Fact]
    public async Task EditEndDate_ShouldUpdateTransferAgreement_WhenInputIsValidAndEndDateIsNull()
    {
        var transferAgreement = new TransferAgreement
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(UserClaimNameOrganizationId),
            EndDate = DateTimeOffset.UtcNow.AddDays(10),
            ReceiverTin = UserClaimNameTin
        };

        mockTransferAgreementRepository.GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin)!.Returns(Task.FromResult(transferAgreement));


        mockTransferAgreementRepository.HasDateOverlap(Arg.Any<TransferAgreement>()).Returns(Task.FromResult(false));


        var newEndDate = new EditTransferAgreementEndDate(null);

        var result = await controller.EditEndDate(transferAgreement.Id, newEndDate);

        result.Result.Should().BeOfType<OkObjectResult>();
        transferAgreement.EndDate.Should().BeNull();
        await mockTransferAgreementRepository.Received(1).GetTransferAgreement(transferAgreement.Id, UserClaimNameOrganizationId, UserClaimNameTin);
        await mockTransferAgreementRepository.Received(1).HasDateOverlap(Arg.Any<TransferAgreement>());
        await mockTransferAgreementRepository.Received(1).Save();
    }
}
