using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using API.Transfer.Api.v2023_11_23.Dto.Responses;
using DataContext.Models;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.v2023_11_23.Controllers;

[UsesVerify]
public class InternalTransferAgreementsControllerTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;
    private readonly HttpClient unauthenticatedClient;

    public InternalTransferAgreementsControllerTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
        unauthenticatedClient = factory.CreateUnauthenticatedClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllTransferAgreements()
    {
        var ta = new TransferAgreement
        {
            EndDate = DateTimeOffset.UtcNow.AddDays(5),
            StartDate = DateTimeOffset.UtcNow,
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = "11111111",
            SenderId = Guid.NewGuid(),
            SenderName = "SomeOrg",
            SenderTin = "11223344",
            TransferAgreementNumber = 1
        };

        await factory.SeedTransferAgreements(new List<TransferAgreement> { ta });

        var response = await unauthenticatedClient.GetFromJsonAsync<InternalTransferAgreementsDto>("api/internal-transfer-agreements/all");

        var settings = new VerifySettings();
        settings.ScrubMembersWithType(typeof(long));
        await Verifier.Verify(response, settings);
    }
}
