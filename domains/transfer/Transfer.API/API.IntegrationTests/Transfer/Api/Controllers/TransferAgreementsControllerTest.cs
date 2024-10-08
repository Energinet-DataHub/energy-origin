using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VerifyTests;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;
using TransferAgreementDto = API.Transfer.Api.Dto.Responses.TransferAgreementDto;
using TransferAgreementsResponse = API.Transfer.Api.Dto.Responses.TransferAgreementsResponse;

namespace API.IntegrationTests.Transfer.Api.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class TransferAgreementsControllerTests
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public TransferAgreementsControllerTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper output)
    {
        factory = integrationTestFixture.Factory;
        this.output = output;
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
    {
        var receiverTin = "12334459";
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedSenderClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedSenderClient, request);

        var receiverSub = Guid.NewGuid();
        var receiverOrgId = Guid.NewGuid();
        var authenticatedReceiverClient = factory.CreateB2CAuthenticatedClient(receiverSub, receiverOrgId, receiverTin);
        var transferAgreement = new CreateTransferAgreement(createdProposalId);
        var response = await authenticatedReceiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={receiverOrgId}", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreementWithSubjectTin_WhenProposalReceiverTinIsNull()
    {
        var subjectTin = "12334455";
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId, subjectTin);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, null);
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedClient, request);

        var transferAgreement = new CreateTransferAgreement(createdProposalId);

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var taStr = await response.Content.ReadAsStringAsync();
        var taDto = JsonConvert.DeserializeObject<TransferAgreementDto>(taStr);

        var get = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/{taDto!.Id}?organizationId={orgId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var taByIdStr = await get.Content.ReadAsStringAsync();
        var taById = JsonConvert.DeserializeObject<TransferAgreementDto>(taByIdStr);

        taById!.ReceiverTin.Should().Be(subjectTin);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenProposalNotFound()
    {
        var transferAgreement = new CreateTransferAgreement(Guid.NewGuid());

        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenProposalIsMeantForAnotherCompany()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin: "32132132");

        var proposalRequest = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, "12341234");
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedClient, proposalRequest);

        var request = new CreateTransferAgreement(createdProposalId);
        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenProposalHasRunOut()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var receiverTin = "12334455";

        var taProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-1),
            StartDate = DateTimeOffset.UtcNow.AddDays(-2),
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = receiverTin,
            SenderCompanyName = "SomeCompany",
            SenderCompanyId = sub,
            SenderCompanyTin = "12345678"
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { taProposal });

        var receiverClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin: receiverTin);

        var createRequest = new CreateTransferAgreement(taProposal.Id);
        var response = await receiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenTransferAgreementAlreadyExists()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var receiverTin = "12334455";

        var ta = new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(5),
            StartDate = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverName = "Prod A/S",
            ReceiverTin = receiverTin,
            SenderId = sub,
            SenderName = "SomeOrg",
            SenderTin = "11223344",
            TransferAgreementNumber = 1
        };

        await SeedTransferAgreements(new List<TransferAgreement> { ta });

        var secondTaProposal = new TransferAgreementProposal
        {
            CreatedAt = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(5),
            Id = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            SenderCompanyName = "SomeOrg",
            ReceiverCompanyTin = receiverTin,
            SenderCompanyId = sub,
            SenderCompanyTin = "11223344"
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { secondTaProposal });

        var receiverClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin: receiverTin);

        var createSecondConnectionResponse = await receiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", new CreateTransferAgreement(secondTaProposal.Id));

        createSecondConnectionResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldDeleteProposal_WhenSuccess()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedSenderClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var receiverTin = "12334455";
        var proposalRequest = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedSenderClient, proposalRequest);

        var receiverSub = Guid.NewGuid();
        var receiverOrgId = Guid.NewGuid();
        var authenticatedReceiverClient = factory.CreateB2CAuthenticatedClient(receiverSub, receiverOrgId, receiverTin);
        await authenticatedReceiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={receiverOrgId}", new CreateTransferAgreement(createdProposalId));

        var getProposalResponse = await authenticatedReceiverClient.GetAsync($"api/transfer/transfer-agreement-proposals/{createdProposalId}?organizationId={receiverOrgId}");

        getProposalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenModelInvalid()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldGetTransferAgreement_WhenOwnerIsValidAndReceiverInvalid()
    {
        var id = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var fakeTransferAgreement = new TransferAgreement
        {
            Id = id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            SenderId = orgId,
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverName = "Hestesko A/S",
            ReceiverTin = "87654321",
            ReceiverReference = Guid.NewGuid()
        };

        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var newAuthenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var get = await newAuthenticatedClient.GetAsync($"api/transfer/transfer-agreements/{id}?organizationId={orgId}");
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
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var fakeTransferAgreement = new TransferAgreement
        {
            Id = id,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(1),
            SenderId = Guid.NewGuid(),
            SenderName = "nrgi A/S",
            SenderTin = "44332211",
            ReceiverName = "Moelle A/S",
            ReceiverTin = receiverTin,
            ReceiverReference = Guid.NewGuid()
        };
        var newAuthenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId, tin: receiverTin);

        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var get = await newAuthenticatedClient.GetAsync($"api/transfer/transfer-agreements/{id}?organizationId={orgId}");
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
        var orgId = Guid.NewGuid();
        var id = Guid.NewGuid();
        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                SenderId = orgId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverName = "Moelle A/S",
                ReceiverTin = "12345678",
                ReceiverReference = orgId
            }
        });

        var otherOrgId = Guid.NewGuid();
        var sub = orgId;
        var newAuthenticatedClient = factory.CreateB2CAuthenticatedClient(sub, otherOrgId, "66778899");

        var get = await newAuthenticatedClient.GetAsync($"api/transfer/transfer-agreements/{id}?organizationId={otherOrgId}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldReturnBadRequest_WhenIdIsInvalidGuid()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/1234?organizationId={orgId}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenResourceIsNotFound()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/{Guid.NewGuid()}?organizationId={orgId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySubjectId_ShouldReturnTransferAgreements_WhenUserHasTransferAgreements()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        await SeedTransferAgreements(
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
                    ReceiverName = "Producent A/S",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow.AddDays(2),
                    EndDate = DateTimeOffset.UtcNow.AddDays(3),
                    SenderId = orgId,
                    SenderName = "Producent A/S",
                    SenderTin = "11223344",
                    ReceiverName = "Test A/S",
                    ReceiverTin = "87654321",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements?organizationId={orgId}");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<TransferAgreementsResponse>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse!.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var receiverTin = "11223344";
        var transferAgreementId = Guid.NewGuid();

        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                SenderId = orgId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverName = "Producent A/S",
                ReceiverTin = receiverTin,
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = orgId,
                StartDate = DateTimeOffset.UtcNow.AddDays(11),
                EndDate = DateTimeOffset.UtcNow.AddDays(15),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverName = "Producent A/S",
                ReceiverTin = receiverTin,
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 2
            }
        });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(13).ToUnixTimeSeconds());
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{transferAgreementId}?organizationId={orgId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var transferAgreementId = Guid.NewGuid();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = transferAgreementId,
                    SenderId = orgId,
                    StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                    EndDate = DateTimeOffset.UtcNow.AddDays(-1),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverName = "Producent A/S",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{transferAgreementId}?organizationId={orgId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenIsNotFoundInDatabase()
    {
        var transferAgreementId = Guid.NewGuid();

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{transferAgreementId}?organizationId={orgId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenTransferAgreementSenderIdDoesNotMatch()
    {
        var transferAgreementId = Guid.NewGuid();

        await SeedTransferAgreements(
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
                    ReceiverName = "Producent A/S",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{transferAgreementId}?organizationId={orgId}", JsonContent.Create(editEndDateRequest));

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
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = agreementId,
                    SenderId = orgId,
                    StartDate = DateTimeOffset.UtcNow.AddDays(1),
                    EndDate = DateTimeOffset.UtcNow.AddDays(10),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverName = "Producent A/S",
                    ReceiverTin = "1122334",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();
        var request = new EditTransferAgreementEndDate(newEndDate);

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{agreementId}?organizationId={orgId}", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        updatedTransferAgreement.Should().NotBeNull();
        updatedTransferAgreement!.EndDate.Should().Be(newEndDate);
    }

    private async Task<Guid> CreateTransferAgreementProposal(Guid orgId, HttpClient authenticatedClient, CreateTransferAgreementProposal request)
    {
        var result = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreement-proposals?organizationId={orgId}", request);
        output.WriteLine(await result.Content.ReadAsStringAsync());
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResponseBody = await result.Content.ReadAsStringAsync();
        var createdProposal = JsonConvert.DeserializeObject<TransferAgreementProposalResponse>(createResponseBody);

        return createdProposal!.Id;
    }

    private async Task SeedTransferAgreements(List<TransferAgreement> transferAgreements)
    {
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        await TestData.SeedTransferAgreements(dbContext, transferAgreements);
    }

    private async Task SeedTransferAgreementProposals(List<TransferAgreementProposal> transferAgreementProposals)
    {
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        await TestData.SeedTransferAgreementProposals(dbContext, transferAgreementProposals);
    }

    [Fact]
    public async Task GetOverviewBySubjectId_ShouldReturnTransferAgreementsOverview_WhenUserHasTransferAgreements()
    {
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new() // Active
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddDays(1),
                    SenderId = Guid.NewGuid(),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverName = "Producent A/S",
                    ReceiverTin = "11223344",
                    ReceiverReference = Guid.NewGuid()
                },
                new() // Inactive
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow.AddDays(2),
                    EndDate = DateTimeOffset.UtcNow.AddDays(3),
                    SenderId = orgId,
                    SenderName = "Producent A/S",
                    SenderTin = "11223344",
                    ReceiverName = "Test A/S",
                    ReceiverTin = "87654321",
                    ReceiverReference = Guid.NewGuid()
                }
            });

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal>
        {
            new () // Proposal
            {
                CreatedAt = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                SenderCompanyName = "SomeOrg",
                ReceiverCompanyTin = "11223342",
                SenderCompanyId = orgId,
                SenderCompanyTin = "11223344"
            },
            new () // Expired
            {
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-16),
                EndDate = DateTimeOffset.UtcNow.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                SenderCompanyName = "SomeOrg",
                ReceiverCompanyTin = "11223342",
                SenderCompanyId = orgId,
                SenderCompanyTin = "11223344"
            }
        });

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/overview?organizationId={orgId}");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<TransferAgreementProposalOverviewResponse>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse!.Result.Should().HaveCount(4);
        transferAgreementsResponse!.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Active)
            .Should().HaveCount(1);
        transferAgreementsResponse!.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Inactive)
            .Should().HaveCount(1);
        transferAgreementsResponse!.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Proposal)
            .Should().HaveCount(1);
        transferAgreementsResponse!.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.ProposalExpired)
            .Should().HaveCount(1);
    }
}
