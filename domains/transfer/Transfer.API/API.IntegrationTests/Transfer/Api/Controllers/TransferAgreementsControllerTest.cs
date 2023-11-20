using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.Transfer.Api.Models;
using API.Transfer.Api.Services;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[UsesVerify]
public class TransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;
    private readonly string sub;

    public TransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;

        sub = Guid.NewGuid().ToString();
        authenticatedClient = factory.CreateAuthenticatedClient(sub);
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
    {
        var receiverTin = "12334455";

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(request);

        var poWalletServiceMock = SetupPoWalletServiceMock();
        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, sub: Guid.NewGuid().ToString(), tin: receiverTin);

        var transferAgreement = new CreateTransferAgreement(createdProposalId);

        var response = await receiverClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenProposalNotFound()
    {
        var transferAgreement = new CreateTransferAgreement(Guid.NewGuid());

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenProposalIsMentForAnotherCompany()
    {
        var proposalRequest = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, "12341234");
        var createdProposalId = await CreateTransferAgreementProposal(proposalRequest);

        var someCompanyId = Guid.NewGuid();
        var someClient = factory.CreateAuthenticatedClient(sub: someCompanyId.ToString(), tin: "32132132");

        var request = new CreateTransferAgreement(createdProposalId);
        var response = await someClient.PostAsJsonAsync("api/transfer-agreements", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenProposalHasRunOut()
    {
        var receiverTin = "12334455";
        var taProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-2),
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = receiverTin,
            SenderCompanyName = "SomeCompany",
            SenderCompanyId = new Guid(sub),
            SenderCompanyTin = "12345678"
        };

        await factory.SeedTransferAgreementProposals(new List<TransferAgreementProposal> { taProposal });

        var receiverClient = factory.CreateAuthenticatedClient(sub: Guid.NewGuid().ToString(), tin: receiverTin);

        var createRequest = new CreateTransferAgreement(taProposal.Id);
        var response = await receiverClient.PostAsJsonAsync("api/transfer-agreements", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenTransferAgreementAlreadyExists()
    {
        var receiverTin = "12334455";

        var ta = new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(5),
            StartDate = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = receiverTin,
            SenderId = new Guid(sub),
            SenderName = "SomeOrg",
            SenderTin = "11223344",
            TransferAgreementNumber = 1
        };

        await factory.SeedTransferAgreements(new List<TransferAgreement> { ta });

        var secondTaProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(5),
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            SenderCompanyName = "SomeOrg",
            ReceiverCompanyTin = receiverTin,
            SenderCompanyId = new Guid(sub),
            SenderCompanyTin = "11223344"
        };

        await factory.SeedTransferAgreementProposals(new List<TransferAgreementProposal> { secondTaProposal });

        var poWalletServiceMock = SetupPoWalletServiceMock();
        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, sub: Guid.NewGuid().ToString(), tin: receiverTin);

        var createSecondConnectionResponse = await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(secondTaProposal.Id));

        createSecondConnectionResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldDeleteProposal_WhenSuccess()
    {
        var receiverTin = "12334455";

        var proposalRequest = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(proposalRequest);

        var poWalletServiceMock = SetupPoWalletServiceMock();
        var receiverClient = factory.CreateAuthenticatedClient(poWalletServiceMock, sub: Guid.NewGuid().ToString(), tin: receiverTin);
        await receiverClient.PostAsJsonAsync("api/transfer-agreements", new CreateTransferAgreement(createdProposalId));

        var getProposalResponse = await receiverClient.GetAsync($"api/transfer-agreement-proposals/{createdProposalId}");

        getProposalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseBody = await getProposalResponse.Content.ReadAsStringAsync();
        responseBody.Should().Be("TransferAgreementProposal expired or deleted");
    }

    [Fact]
    public async Task Create_ShouldFail_WhenModelInvalid()
    {
        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldGetTransferAgreement_WhenOwnerIsValidAndReceiverInvalid()
    {
        var id = Guid.NewGuid();
        var subject = Guid.NewGuid();
        var fakeTransferAgreement = new TransferAgreement
        {
            Id = id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            SenderId = subject,
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverTin = "87654321",
            ReceiverReference = Guid.NewGuid()
        };

        await factory.SeedTransferAgreements(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: subject.ToString(), tin: "");
        var get = await newAuthenticatedClient.GetAsync($"api/transfer-agreements/{id}");
        get.EnsureSuccessStatusCode();

        var getTransferAgreement = JsonConvert.DeserializeObject<TransferAgreementDto>(await get.Content.ReadAsStringAsync());

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(long));

        await Verifier.Verify(getTransferAgreement, settings);
    }

    [Fact]
    public async Task Get_ShouldGetTransferAgreement_WhenReceiverIsValidAndOwnerIsInvalid()
    {
        var id = Guid.NewGuid();
        var receiverTin = "12345678";
        var subject = Guid.NewGuid();
        var fakeTransferAgreement = new TransferAgreement
        {
            Id = id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            SenderId = Guid.NewGuid(),
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverTin = receiverTin,
            ReceiverReference = Guid.NewGuid()
        };
        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: subject.ToString(), tin: receiverTin);

        await factory.SeedTransferAgreements(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var get = await newAuthenticatedClient.GetAsync($"api/transfer-agreements/{id}");
        get.EnsureSuccessStatusCode();

        var getTransferAgreement = JsonConvert.DeserializeObject<TransferAgreementDto>(await get.Content.ReadAsStringAsync());
        getTransferAgreement.Should().NotBeNull();

        getTransferAgreement.Should().NotBeNull();

        getTransferAgreement!.Id.Should().Be(fakeTransferAgreement.Id);
        getTransferAgreement.ReceiverTin.Should().Be(fakeTransferAgreement.ReceiverTin);
        getTransferAgreement.StartDate.Should().Be(fakeTransferAgreement.StartDate.ToUnixTimeSeconds());
        getTransferAgreement.EndDate.Should().Be(fakeTransferAgreement.EndDate?.ToUnixTimeSeconds());
        getTransferAgreement.SenderName.Should().Be(fakeTransferAgreement.SenderName);
        getTransferAgreement.SenderTin.Should().Be(fakeTransferAgreement.SenderTin);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenYourNotTheOwnerOrReceiver()
    {
        var id = Guid.NewGuid();
        await factory.SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                SenderId = Guid.NewGuid(),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid()
            }
        });

        var newOwner = Guid.NewGuid().ToString();
        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: newOwner, tin: "");

        var get = await newAuthenticatedClient.GetAsync($"api/transfer-agreements/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnBadRequest_WhenIdIsInvalidGuid()
    {
        var response = await authenticatedClient.GetAsync("api/transfer-agreements/1234");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenResourceIsNotFound()
    {
        var response = await authenticatedClient.GetAsync($"api/transfer-agreements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySubjectId_ShouldReturnTransferAgreements_WhenUserHasTransferAgreements()
    {
        await factory.SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddDays(1),
                    SenderId = Guid.NewGuid(),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow.AddDays(2),
                    EndDate = DateTimeOffset.UtcNow.AddDays(3),
                    SenderId = Guid.Parse(sub),
                    SenderName = "Producent A/S",
                    SenderTin = "11223344",
                    ReceiverTin = "87654321",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var response = await authenticatedClient.GetAsync("api/transfer-agreements");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<TransferAgreementsResponse>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var receiverTin = "11223344";
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                SenderId = Guid.Parse(sub),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin,
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse(sub),
                StartDate = DateTimeOffset.UtcNow.AddDays(11),
                EndDate = DateTimeOffset.UtcNow.AddDays(15),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin,
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 2
            }
        });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(13).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflictErrorMessage = await response.Content.ReadAsStringAsync();
        conflictErrorMessage.Should().Be("Transfer agreement date overlap");
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = transferAgreementId,
                    SenderId = Guid.Parse(sub),
                    StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                    EndDate = DateTimeOffset.UtcNow.AddDays(-1),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().NotBeNullOrEmpty();
        validationProblemContent.Should().Contain("Transfer agreement has expired");
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenIsNotFoundInDatabase()
    {
        var transferAgreementId = Guid.NewGuid();

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenTransferAgreementSenderIdDoesNotMatch()
    {
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = transferAgreementId,
                    SenderId = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                    EndDate = DateTimeOffset.UtcNow.AddDays(-1),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void EditTransferAgreementEndDateValidator_ShouldValidateEndDateGreaterThanCurrentTimeStamp()
    {
        var validator = new EditTransferAgreementEndDateValidator();
        var request = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds());

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("EndDate");
    }

    [Fact]
    public void EditTransferAgreementEndDateValidator_ShouldValidateMustBeBeforeYear10000()
    {
        var validator = new EditTransferAgreementEndDateValidator();
        var request = new EditTransferAgreementEndDate(253402300800);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("EndDate");
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain("seconds");
    }

    [Fact]
    public async Task EditEndDate_ShouldUpdateTransferAgreement_WhenInputIsValid()
    {
        var agreementId = Guid.NewGuid();

        await factory.SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = agreementId,
                    SenderId = Guid.Parse(sub),
                    StartDate = DateTimeOffset.UtcNow.AddDays(1),
                    EndDate = DateTimeOffset.UtcNow.AddDays(10),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "1122334",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();
        var request = new EditTransferAgreementEndDate(newEndDate);

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{agreementId}", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        updatedTransferAgreement.Should().NotBeNull();
        updatedTransferAgreement!.EndDate.Should().Be(newEndDate);
    }

    private async Task<Guid> CreateTransferAgreementProposal(CreateTransferAgreementProposal request)
    {
        var result = await authenticatedClient.PostAsJsonAsync("api/transfer-agreement-proposals", request);
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposal>(createResponseBody);

        return createdProposal!.Id;
    }

    private IProjectOriginWalletService SetupPoWalletServiceMock()
    {
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        poWalletServiceMock.CreateWalletDepositEndpoint(Arg.Any<string>()).Returns("SomeToken");
        poWalletServiceMock.CreateReceiverDepositEndpoint(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Guid.NewGuid());

        return poWalletServiceMock;
    }
}
