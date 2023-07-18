using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.IntegrationTests.Factories;
using FluentAssertions;
using Newtonsoft.Json;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Controllers;

[UsesVerify]
public class TransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient authenticatedClient;

    public TransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;

        var sub = Guid.NewGuid().ToString();
        authenticatedClient = factory.CreateAuthenticatedClient(sub);
    }

    [Fact]
    public async Task Create_ShouldCreateTransferAgreement_WhenModelIsValid()
    {
        var transferAgreement = CreateTransferAgreement();

        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", transferAgreement);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_ShouldFail_WhenModelInvalid()
    {
        var response = await authenticatedClient.PostAsJsonAsync("api/transfer-agreements", new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenStartDateOrEndDateCauseOverlap()
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());
        var id = Guid.NewGuid();

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = senderId,
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678"
            }
        });

        var overlappingRequest = new CreateTransferAgreement(
            StartDate: DateTimeOffset.UtcNow.AddDays(4).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds(),
            ReceiverTin: "12345678"
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreements", JsonContent.Create(overlappingRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(-1, null, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(-1, 4, HttpStatusCode.BadRequest, "Start Date cannot be in the past")]
    [InlineData(3, 1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    [InlineData(0, -1, HttpStatusCode.BadRequest, "End Date must be null or later than Start Date")]
    public async Task Create_ShouldFail_WhenStartOrEndDateInvalid(int startDayOffset, int? endDayOffset, HttpStatusCode expectedStatusCode, string expectedContent)
    {
        var senderId = Guid.NewGuid();
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var now = DateTimeOffset.UtcNow;

        var startDate = now.AddDays(startDayOffset).ToUnixTimeSeconds();
        var endDate = endDayOffset.HasValue ? now.AddDays(endDayOffset.Value).ToUnixTimeSeconds() : (long?)null;

        var request = new CreateTransferAgreement(
            StartDate: startDate,
            EndDate: endDate,
            ReceiverTin: "12345678"
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreements", JsonContent.Create(request));

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(expectedStatusCode);
        validationProblemContent.Should().Contain(expectedContent);
    }

    [Theory]
    [InlineData(253402300800L, null, "StartDate")]
    [InlineData(221860025546L, 253402300800L, "EndDate")]
    public async Task CreateTransferAgreement_ShouldFail_WhenDateInvalid(long start, long? end, string property)
    {
        var senderId = Guid.NewGuid();
        var senderTin = "11223344";
        var receiverTin = "12345678";
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString(), tin: senderTin);

        var request = new CreateTransferAgreement(
            StartDate: start,
            EndDate: end,
            ReceiverTin: receiverTin
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreements", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain("too high! Please make sure the format is UTC in seconds.");
        validationProblemContent.Should().Contain(property);
    }


    [Theory]
    [InlineData("", "ReceiverTin cannot be empty")]
    [InlineData("1234567", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("123456789", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("ABCDEFG", "ReceiverTin must be 8 digits without any spaces.")]
    [InlineData("11223344", "ReceiverTin cannot be the same as SenderTin.")]
    public async Task Create_ShouldFail_WhenReceiverTinInvalid(string tin, string expectedContent)
    {
        var senderId = Guid.NewGuid();
        var senderTin = "11223344";
        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString(), tin: senderTin);

        var request = new CreateTransferAgreement(
            StartDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            ReceiverTin: tin
        );

        var response = await authenticatedClient.PostAsync("api/transfer-agreements", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validationProblemContent = await response.Content.ReadAsStringAsync();

        validationProblemContent.Should().Contain(expectedContent);
        validationProblemContent.Should().Contain("ReceiverTin");
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
            ReceiverTin = "87654321"
        };

        await factory.SeedData(new List<TransferAgreement>()
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
            ReceiverTin = receiverTin
        };
        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: subject.ToString(), tin: receiverTin);

        await factory.SeedData(new List<TransferAgreement>()
        {
            fakeTransferAgreement
        });

        var get = await newAuthenticatedClient.GetAsync($"api/transfer-agreements/{id}");
        get.EnsureSuccessStatusCode();

        var getTransferAgreement = JsonConvert.DeserializeObject<TransferAgreementDto>(await get.Content.ReadAsStringAsync());
        getTransferAgreement.Should().NotBeNull();

        getTransferAgreement.Should().NotBeNull();

        getTransferAgreement.Id.Should().Be(fakeTransferAgreement.Id);
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
        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = id,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(1),
                SenderId = Guid.NewGuid(),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678"
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

    private static CreateTransferAgreement CreateTransferAgreement()
    {
        return new CreateTransferAgreement(DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(), "12345678");
    }

    [Fact]
    public async Task GetBySubjectId_ShouldReturnTransferAgreements_WhenUserHasTransferAgreements()
    {
        var sub = Guid.NewGuid().ToString();
        var senderTin = "11223344";
        var receiverTin = "11223344";

        await factory.SeedData(
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
                    ReceiverTin = receiverTin
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    StartDate = DateTimeOffset.UtcNow.AddDays(2),
                    EndDate = DateTimeOffset.UtcNow.AddDays(3),
                    SenderId = Guid.Parse(sub),
                    SenderName = "Producent A/S",
                    SenderTin = senderTin,
                    ReceiverTin = "87654321"
                }
            });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub, tin: receiverTin);

        var response = await authenticatedClient.GetAsync("api/transfer-agreements");

        response.EnsureSuccessStatusCode();
        var transferAgreements = await response.Content.ReadAsStringAsync();
        var transferAgreementsResponse = JsonConvert.DeserializeObject<TransferAgreementsResponse>(transferAgreements);

        transferAgreementsResponse.Should().NotBeNull();
        transferAgreementsResponse.Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnConflict_WhenNewEndDateCausesOverlap()
    {
        var receiverTin = "11223344";
        var senderId = Guid.NewGuid();
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedData(new List<TransferAgreement>()
        {
            new()
            {
                Id = transferAgreementId,
                SenderId = senderId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin
            },
            new()
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                StartDate = DateTimeOffset.UtcNow.AddDays(11),
                EndDate = DateTimeOffset.UtcNow.AddDays(15),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = receiverTin
            }
        });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(13).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflictErrorMessage = await response.Content.ReadAsStringAsync();
        conflictErrorMessage.Should().Be("Transfer agreement date overlap");
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnValidationProblem_WhenTransferAgreementExpired()
    {
        var senderId = Guid.NewGuid();
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedData(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = transferAgreementId,
                    SenderId = senderId,
                    StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                    EndDate = DateTimeOffset.UtcNow.AddDays(-1),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "11223344"
                }
            });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

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
        var senderId = Guid.NewGuid();
        var transferAgreementId = Guid.NewGuid();

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var editEndDateRequest = new EditTransferAgreementEndDate(DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds());

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{transferAgreementId}", JsonContent.Create(editEndDateRequest));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEndDate_ShouldReturnNotFound_WhenTransferAgreementSenderIdDoesNotMatch()
    {
        var senderId = Guid.NewGuid();
        var transferAgreementId = Guid.NewGuid();

        await factory.SeedData(
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
                    ReceiverTin = "11223344"
                }
            });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

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
        var senderId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();

        await factory.SeedData(
            new List<TransferAgreement>()
            {
                new()
                {
                    Id = agreementId,
                    SenderId = senderId,
                    StartDate = DateTimeOffset.UtcNow.AddDays(1),
                    EndDate = DateTimeOffset.UtcNow.AddDays(10),
                    SenderName = "nrgi A/S",
                    SenderTin = "44332211",
                    ReceiverTin = "1122334"
                }
            });

        var authenticatedClient = factory.CreateAuthenticatedClient(sub: senderId.ToString());

        var newEndDate = DateTimeOffset.UtcNow.AddDays(15).ToUnixTimeSeconds();
        var request = new EditTransferAgreementEndDate(newEndDate);

        var response = await authenticatedClient.PatchAsync($"api/transfer-agreements/{agreementId}", JsonContent.Create(request));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTransferAgreement = await response.Content.ReadFromJsonAsync<TransferAgreementDto>();
        updatedTransferAgreement.Should().NotBeNull();
        updatedTransferAgreement.EndDate.Should().Be(newEndDate);
    }

    [Fact]
    public async Task CreateWalletDepositEndpoint_ShouldReturnBase64_WhenAuthorized()
    {
        var subject = Guid.NewGuid();
        var newAuthenticatedClient = factory.CreateAuthenticatedClient(sub: subject.ToString(), tin: "");

        var response = await newAuthenticatedClient.PostAsync("api/transfer-agreements/wallet-deposit-endpoint", null);
        response.EnsureSuccessStatusCode();

        Assert.NotNull(response.Content);
        var base64String = await response.Content.ReadAsStringAsync();
        Assert.True(IsBase64String(base64String));
    }

    private bool IsBase64String(string input)
    {
        input = input.Replace(" ", "");
        return (input.Length % 4 == 0) && Regex.IsMatch(input, @"^[a-zA-Z0-9+/]*={0,3}$");
    }
}
