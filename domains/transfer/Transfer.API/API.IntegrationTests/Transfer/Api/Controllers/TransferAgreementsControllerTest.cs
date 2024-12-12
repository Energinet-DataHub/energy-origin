using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api._Features_;
using API.Transfer.Api.Clients;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Domain.ValueObjects.Tests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VerifyTests;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

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
    public async Task GivenProposal_WhenAccepting_ReceiverIdIsStored()
    {
        // Given proposal
        var receiverTin = "12334459";
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedSenderClient = factory.CreateB2CAuthenticatedClient(sub, orgId);
        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedSenderClient, request);

        // When accepting proposal
        var receiverSub = Guid.NewGuid();
        var receiverOrgId = Guid.NewGuid();
        var authenticatedReceiverClient = factory.CreateB2CAuthenticatedClient(receiverSub, receiverOrgId, receiverTin);
        var transferAgreement = new CreateTransferAgreement(createdProposalId);
        var response = await authenticatedReceiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={receiverOrgId}", transferAgreement);

        // Response is OK, and transfer agreement is created with receiver id
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        var storedTransferAgreement = dbContext.TransferAgreements.Single(x => x.SenderId == OrganizationId.Create(orgId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        storedTransferAgreement.ReceiverId!.Value.Should().Be(receiverOrgId);
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
        var orgId = Any.OrganizationId();
        var receiverTin = Tin.Create("12334455");

        var taProposal = new TransferAgreementProposal
        {
            CreatedAt = UnixTimestamp.Now(),
            EndDate = UnixTimestamp.Now().AddDays(-1),
            StartDate = UnixTimestamp.Now().AddDays(-2),
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = receiverTin,
            SenderCompanyName = OrganizationName.Create("SomeCompany"),
            SenderCompanyId = Any.OrganizationId(),
            SenderCompanyTin = Tin.Create("12345678")
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { taProposal });

        var receiverClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value, tin: receiverTin.Value);

        var createRequest = new CreateTransferAgreement(taProposal.Id);
        var response = await receiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={orgId}", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnConflict_WhenTransferAgreementAlreadyExists()
    {
        var sub = Guid.NewGuid();
        var orgId = Any.OrganizationId();
        var receiverTin = Tin.Create("12334455");

        var ta = new TransferAgreement
        {
            EndDate = UnixTimestamp.Now().AddDays(5),
            StartDate = UnixTimestamp.Now(),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverName = OrganizationName.Create("Prod A/S"),
            ReceiverTin = receiverTin,
            SenderId = orgId,
            SenderName = OrganizationName.Create("SomeOrg"),
            SenderTin = Tin.Create("11223344"),
            TransferAgreementNumber = 1
        };

        await SeedTransferAgreements(new List<TransferAgreement> { ta });

        var secondTaProposal = new TransferAgreementProposal
        {
            CreatedAt = UnixTimestamp.Now(),
            EndDate = UnixTimestamp.Now().AddDays(5),
            Id = Guid.NewGuid(),
            StartDate = UnixTimestamp.Now(),
            SenderCompanyName = OrganizationName.Create("SomeOrg"),
            ReceiverCompanyTin = receiverTin,
            SenderCompanyId = orgId,
            SenderCompanyTin = Tin.Create("11223344")
        };

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal> { secondTaProposal });

        var receiverClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value, tin: receiverTin.Value);

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
        var orgId = OrganizationId.Create(Guid.NewGuid());
        var fakeTransferAgreement = new TransferAgreement
        {
            Id = id,
            StartDate = UnixTimestamp.Now(),
            EndDate = UnixTimestamp.Now().AddDays(1),
            SenderId = orgId,
            SenderName = OrganizationName.Create("nrgi A/S"),
            SenderTin = Tin.Create("44332211"),
            ReceiverName = OrganizationName.Create("Hestesko A/S"),
            ReceiverTin = Tin.Create("87654321"),
            ReceiverReference = Guid.NewGuid()
        };

        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var newAuthenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value);
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
            StartDate = UnixTimestamp.Now(),
            EndDate = UnixTimestamp.Now().AddDays(1),
            SenderId = Any.OrganizationId(),
            SenderName = OrganizationName.Create("nrgi A/S"),
            SenderTin = Tin.Create("44332211"),
            ReceiverName = OrganizationName.Create("Moelle A/S"),
            ReceiverTin = Tin.Create(receiverTin),
            ReceiverReference = Any.Guid()
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
        getTransferAgreement.ReceiverTin.Should().Be(fakeTransferAgreement.ReceiverTin.Value);
        getTransferAgreement.StartDate.Should().Be(fakeTransferAgreement.StartDate.EpochSeconds);
        getTransferAgreement.EndDate.Should().Be(fakeTransferAgreement.EndDate?.EpochSeconds);
        getTransferAgreement.SenderName.Should().Be(fakeTransferAgreement.SenderName.Value);
        getTransferAgreement.SenderTin.Should().Be(fakeTransferAgreement.SenderTin.Value);
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenYourNotTheOwnerOrReceiver()
    {
        var orgId = Any.OrganizationId();
        var id = Guid.NewGuid();
        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = UnixTimestamp.Now(),
                EndDate = UnixTimestamp.Now().AddDays(1),
                SenderId = orgId,
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverName = OrganizationName.Create("Moelle A/S"),
                ReceiverTin = Tin.Create("12345678"),
                ReceiverReference = Any.Guid()
            }
        });

        var otherOrgId = Any.OrganizationId();
        var sub = Guid.NewGuid();
        var newAuthenticatedClient = factory.CreateB2CAuthenticatedClient(sub, otherOrgId.Value, "66778899");

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
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var sub = Guid.NewGuid();
        var orgId = Any.OrganizationId();
        var receiverTin = "11223344";
        var transferAgreementId = Guid.NewGuid();

        await SeedTransferAgreements(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                SenderId = orgId,
                StartDate = UnixTimestamp.Now(),
                EndDate = UnixTimestamp.Now().AddDays(10),
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverName = OrganizationName.Create("Producent A/S"),
                ReceiverTin = Tin.Create(receiverTin),
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = orgId,
                StartDate = UnixTimestamp.Now().AddDays(11),
                EndDate = UnixTimestamp.Now().AddDays(15),
                SenderName = OrganizationName.Create("nrgi A/S"),
                SenderTin = Tin.Create("44332211"),
                ReceiverName = OrganizationName.Create("Producent A/S"),
                ReceiverTin = Tin.Create(receiverTin),
                ReceiverReference = Guid.NewGuid(),
                TransferAgreementNumber = 2
            }
        });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(13).ToUnixTimeSeconds());
        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value);
        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{transferAgreementId}?organizationId={orgId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var sub = Guid.NewGuid();
        var orgId = Any.OrganizationId();
        var transferAgreementId = Guid.NewGuid();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = transferAgreementId,
                    SenderId = orgId,
                    StartDate = UnixTimestamp.Now().AddDays(-5),
                    EndDate = UnixTimestamp.Now().AddDays(-1),
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332211"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = Tin.Create("11223344"),
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value);
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
                    SenderId = OrganizationId.Create(Guid.NewGuid()),
                    StartDate = UnixTimestamp.Now().AddDays(-5),
                    EndDate = UnixTimestamp.Now().AddDays(-1),
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332211"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = Tin.Create("11223344"),
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
        var orgId = Any.OrganizationId();
        var agreementId = Guid.NewGuid();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = agreementId,
                    SenderId = orgId,
                    StartDate = UnixTimestamp.Now().AddDays(1),
                    EndDate = UnixTimestamp.Now().AddDays(10),
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332211"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = Tin.Create("11223344"),
                    ReceiverReference = Guid.NewGuid()
                }
            });

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();
        var request = new EditTransferAgreementEndDate(newEndDate);

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub, orgId.Value);
        var response = await authenticatedClient.PutAsync($"api/transfer/transfer-agreements/{agreementId}?organizationId={orgId}", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        updatedTransferAgreement.Should().NotBeNull();
        updatedTransferAgreement!.EndDate.Should().Be(newEndDate);
    }

    [Fact]
    public async Task CreatePOATransferAgreement_ShouldCreateTransferAgreement_WhenInputIsValid()
    {
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;

        var senderOrganizationId = Guid.NewGuid();
        var receiverOrganizationId = Guid.NewGuid();

        MockAuthorizationClient.MockedConsents = new List<UserOrganizationConsentsResponseItem>()
        {
            new UserOrganizationConsentsResponseItem(
                Guid.NewGuid(),
                receiverOrganizationId,
                "12345678",
                "A",
                senderOrganizationId,
                "87654321",
                "B",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            ),
            new UserOrganizationConsentsResponseItem(
                System.Guid.NewGuid(),
                senderOrganizationId, // Sender
                "87654321",
                "B",
                receiverOrganizationId,
                "12345678",
                "A",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            )
        };

        var request = new CreateTransferAgreementRequest(receiverOrganizationId, senderOrganizationId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), CreateTransferAgreementType.TransferCertificatesBasedOnConsumption);


        var authenticatedClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), orgIds: $"{senderOrganizationId} {receiverOrganizationId}");
        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements/create", request);

        var transferAgreement = dbContext.TransferAgreements.SingleOrDefault(x => x.SenderId == OrganizationId.Create(senderOrganizationId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        transferAgreement!.SenderName.Value.Should().Be(MockAuthorizationClient.MockedConsents.Single(x => x.GiverOrganizationId == senderOrganizationId).GiverOrganizationName); // MockAuthorizationClientName
        transferAgreement!.ReceiverId!.Value.Should().Be(receiverOrganizationId);
    }

    [Fact]
    public async Task CreatePOATransferAgreementWithSelf_ShouldCreateTransferAgreement_WhenInputIsValid()
    {
        var senderOrganizationId = Guid.NewGuid();
        var receiverOrganizationId = Guid.NewGuid();

        MockAuthorizationClient.MockedConsents = new List<UserOrganizationConsentsResponseItem>()
        {
            new UserOrganizationConsentsResponseItem(
                Guid.NewGuid(),
                receiverOrganizationId,
                "12345678",
                "A",
                senderOrganizationId,
                "87654321",
                "B",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            ),
            new UserOrganizationConsentsResponseItem(
                System.Guid.NewGuid(),
                senderOrganizationId, // Sender
                "87654321",
                "B",
                receiverOrganizationId,
                "12345678",
                "A",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            )
        };

        var request = new CreateTransferAgreementRequest(receiverOrganizationId, senderOrganizationId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), CreateTransferAgreementType.TransferCertificatesBasedOnConsumption);
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(Guid.NewGuid(), senderOrganizationId, orgIds: $"{receiverOrganizationId}");
        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements/create", request);

        var transferAgreement = dbContext.TransferAgreements.SingleOrDefault(x => x.SenderId == OrganizationId.Create(senderOrganizationId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        transferAgreement!.SenderName.Value.Should().Be("Producent A/S");
        transferAgreement!.ReceiverId!.Value.Should().Be(receiverOrganizationId);
    }

    [Fact]
    public async Task CreatePOATransferAgreement_ShouldFailTransferAgreement_WhenThereIsAlreadyOneOverlappingTransferAgreement()
    {
        var senderOrganizationId = Guid.NewGuid();
        var receiverOrganizationId = Guid.NewGuid();

        MockAuthorizationClient.MockedConsents = new List<UserOrganizationConsentsResponseItem>()
        {
            new UserOrganizationConsentsResponseItem(
                Guid.NewGuid(),
                receiverOrganizationId,
                "12345678",
                "A",
                senderOrganizationId,
                "87654321",
                "B",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            ),
            new UserOrganizationConsentsResponseItem(
                System.Guid.NewGuid(),
                senderOrganizationId, // Sender
                "87654321",
                "B",
                receiverOrganizationId,
                "12345678",
                "A",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            )
        };
        var agreementId = Guid.NewGuid();

        var existingTransferAgreement = new TransferAgreement()
        {
            Id = agreementId,
            StartDate = UnixTimestamp.Now().AddDays(1),
            EndDate = UnixTimestamp.Now().AddDays(20),
            SenderId = OrganizationId.Create(senderOrganizationId),
            SenderName = OrganizationName.Create("nrgi A/S"),
            SenderTin = Tin.Create("87654321"),
            ReceiverId = OrganizationId.Create(receiverOrganizationId),
            ReceiverName = OrganizationName.Create("Producent A/S"),
            ReceiverTin = Tin.Create("12345678"),
            ReceiverReference = Guid.NewGuid()
        };
        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                existingTransferAgreement
            });

        var request = new CreateTransferAgreementRequest(receiverOrganizationId, senderOrganizationId, UnixTimestamp.Now().EpochSeconds, UnixTimestamp.Now().AddDays(10).EpochSeconds, CreateTransferAgreementType.TransferCertificatesBasedOnConsumption);
        var jsonContent = JsonContent.Create(request);
        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(receiverOrganizationId, senderOrganizationId, orgIds: $"{senderOrganizationId}, {receiverOrganizationId}");
        var response = await authenticatedClient.PostAsJsonAsync($"api/transfer/transfer-agreements/create", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
    public async Task GetOnlyConsentTransferAgreement()
    {
        var sub = Guid.NewGuid();
        var orgId = Any.OrganizationId();
        var consentOrgId = Any.OrganizationId();
        var tin = Tin.Create("55667788");
        var now = UnixTimestamp.Now();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new() // Inactive Own TA
                {
                    Id = Guid.NewGuid(),
                    StartDate = now.AddHours(4),
                    EndDate = now.AddDays(5),
                    SenderId = orgId,
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332233"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = tin,
                    ReceiverReference = Guid.NewGuid(),
                    Type = TransferAgreementType.TransferAllCertificates
                },
                new() // Active
                {
                    Id = Guid.NewGuid(),
                    StartDate = now.AddHours(-1),
                    EndDate = now.AddDays(1),
                    SenderId = consentOrgId,
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332233"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = tin,
                    ReceiverReference = Guid.NewGuid(),
                    Type = TransferAgreementType.TransferAllCertificates
                },
                new() // Inactive Receiver
                {
                    Id = Guid.NewGuid(),
                    StartDate = now.AddDays(2),
                    EndDate = now.AddDays(3),
                    SenderId = Any.OrganizationId(),
                    SenderName = OrganizationName.Create("Producent A/S"),
                    SenderTin = tin,
                    ReceiverName = OrganizationName.Create("Test A/S"),
                    ReceiverTin = Tin.Create("87654321"),
                    ReceiverId = consentOrgId,
                    ReceiverReference = Guid.NewGuid(),
                    Type = TransferAgreementType.TransferCertificatesBasedOnConsumption
                }
            });

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal>
        {
            new () // Proposal
            {
                CreatedAt = now.AddDays(-1),
                EndDate = now.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = now.AddDays(1),
                SenderCompanyName = OrganizationName.Create("SomeOrg"),
                ReceiverCompanyTin = Tin.Create("11223342"),
                SenderCompanyId = consentOrgId,
                SenderCompanyTin = tin,
                Type = TransferAgreementType.TransferAllCertificates
            },
            new () // Expired
            {
                CreatedAt = now.AddDays(-16),
                EndDate = now.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = now,
                SenderCompanyName = OrganizationName.Create("SomeOrg"),
                ReceiverCompanyTin = Tin.Create("11223342"),
                SenderCompanyId = consentOrgId,
                SenderCompanyTin = tin,
                Type = TransferAgreementType.TransferCertificatesBasedOnConsumption
            }
        });

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub: sub, orgId: orgId.Value, orgIds: consentOrgId.Value.ToString(), tin: tin.Value);
        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/overview/consent");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<GetTransferAgreementQueryResult>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse!.Result.Should().HaveCount(4);
        transferAgreementsResponse.Result.Where(ta => ta.Type == TransferAgreementTypeDto.TransferAllCertificates).Should().HaveCount(2);
        transferAgreementsResponse.Result.Where(ta => ta.Type == TransferAgreementTypeDto.TransferCertificatesBasedOnConsumption).Should().HaveCount(2);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Active)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Inactive)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Proposal)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.ProposalExpired)
            .Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOwnOverviewBySubjectId_ShouldReturnTransferAgreementsOverview_WhenUserHasTransferAgreements()
    {
        var sub = Guid.NewGuid();
        var orgId = Any.OrganizationId();
        var consentOrgId = Any.OrganizationId();
        var tin = Tin.Create("55667788");
        var now = UnixTimestamp.Now();

        await SeedTransferAgreements(
            new List<TransferAgreement>()
            {
                new() // Active
                {
                    Id = Guid.NewGuid(),
                    StartDate = now.AddHours(-1),
                    EndDate = now.AddDays(1),
                    SenderId = OrganizationId.Create(Guid.NewGuid()),
                    SenderName = OrganizationName.Create("nrgi A/S"),
                    SenderTin = Tin.Create("44332233"),
                    ReceiverName = OrganizationName.Create("Producent A/S"),
                    ReceiverTin = tin,
                    ReceiverReference = Guid.NewGuid(),
                    Type = TransferAgreementType.TransferAllCertificates
                },
                new() // Inactive
                {
                    Id = Guid.NewGuid(),
                    StartDate = now.AddDays(2),
                    EndDate = now.AddDays(3),
                    SenderId = orgId,
                    SenderName = OrganizationName.Create("Producent A/S"),
                    SenderTin = tin,
                    ReceiverName = OrganizationName.Create("Test A/S"),
                    ReceiverTin = Tin.Create("87654321"),
                    ReceiverReference = Guid.NewGuid(),
                    Type = TransferAgreementType.TransferCertificatesBasedOnConsumption
                },
                new() // Inactive Consent TA
                {
                Id = Guid.NewGuid(),
                StartDate = now.AddDays(2),
                EndDate = now.AddDays(3),
                SenderId = consentOrgId,
                SenderName = OrganizationName.Create("Producent A/S"),
                SenderTin = tin,
                ReceiverName = OrganizationName.Create("Test A/S"),
                ReceiverTin = Tin.Create("87654321"),
                ReceiverReference = Guid.NewGuid(),
                Type = TransferAgreementType.TransferCertificatesBasedOnConsumption
            }
            });

        await SeedTransferAgreementProposals(new List<TransferAgreementProposal>
        {
            new () // Proposal
            {
                CreatedAt = now.AddDays(-1),
                EndDate = now.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = now.AddDays(1),
                SenderCompanyName = OrganizationName.Create("SomeOrg"),
                ReceiverCompanyTin = Tin.Create("11223342"),
                SenderCompanyId = orgId,
                SenderCompanyTin = tin,
                Type = TransferAgreementType.TransferAllCertificates
            },
            new () // Expired
            {
                CreatedAt = now.AddDays(-16),
                EndDate = now.AddDays(5),
                Id = Guid.NewGuid(),
                StartDate = now,
                SenderCompanyName = OrganizationName.Create("SomeOrg"),
                ReceiverCompanyTin = Tin.Create("11223342"),
                SenderCompanyId = orgId,
                SenderCompanyTin = tin,
                Type = TransferAgreementType.TransferCertificatesBasedOnConsumption
            }
        });

        var authenticatedClient = factory.CreateB2CAuthenticatedClient(sub: sub, orgId: orgId.Value, orgIds: consentOrgId.Value.ToString(), tin: tin.Value);
        var response = await authenticatedClient.GetAsync($"api/transfer/transfer-agreements/overview?organizationId={orgId}");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<GetTransferAgreementQueryResult>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse!.Result.Should().HaveCount(4);
        transferAgreementsResponse.Result.Where(ta => ta.Type == TransferAgreementTypeDto.TransferAllCertificates).Should().HaveCount(2);
        transferAgreementsResponse.Result.Where(ta => ta.Type == TransferAgreementTypeDto.TransferCertificatesBasedOnConsumption).Should().HaveCount(2);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Active)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Inactive)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.Proposal)
            .Should().HaveCount(1);
        transferAgreementsResponse.Result.Where(x => x.TransferAgreementStatus == TransferAgreementStatus.ProposalExpired)
            .Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreementForConsent_WhenModelIsValid()
    {
        var senderOrganizationId = Guid.NewGuid();
        var receiverOrganizationId = Guid.NewGuid();

        MockAuthorizationClient.MockedConsents = new List<UserOrganizationConsentsResponseItem>()
        {
            new UserOrganizationConsentsResponseItem(
                Guid.NewGuid(),
                receiverOrganizationId,
                "12345678",
                "A",
                senderOrganizationId,
                "87654321",
                "B",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            ),
            new UserOrganizationConsentsResponseItem(
                System.Guid.NewGuid(),
                senderOrganizationId, // Sender
                "87654321",
                "B",
                receiverOrganizationId,
                "12345678",
                "A",
                UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
            )
        };

        var receiverTin = MockAuthorizationClient.MockedConsents.First().GiverOrganizationTin;
        var sub = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var authenticatedSenderClient = factory.CreateB2CAuthenticatedClient(sub, orgId);

        var request = new CreateTransferAgreementProposal(DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds(), null, receiverTin);
        var createdProposalId = await CreateTransferAgreementProposal(orgId, authenticatedSenderClient, request);

        var anySub = Guid.NewGuid();
        var anyOrgId = Guid.NewGuid();
        var anyTin = "98989898";
        var authenticatedReceiverClient = factory.CreateB2CAuthenticatedClient(anySub, anyOrgId, anyTin, orgIds: $"{senderOrganizationId}, {receiverOrganizationId}");
        var transferAgreement = new CreateTransferAgreement(createdProposalId);
        var response = await authenticatedReceiverClient.PostAsJsonAsync($"api/transfer/transfer-agreements?organizationId={receiverOrganizationId}", transferAgreement);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        var storedTransferAgreement = dbContext.TransferAgreements.Single(x => x.SenderId == OrganizationId.Create(orgId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        storedTransferAgreement.ReceiverId!.Value.Should().Be(receiverOrganizationId);
    }
}
