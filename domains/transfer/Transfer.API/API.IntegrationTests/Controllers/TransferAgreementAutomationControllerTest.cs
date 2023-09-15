using System;
using System.Net.Http;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.Controllers;
using API.IntegrationTests.Factories;
using API.TransferAgreementsAutomation;
using Argon;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class TransferAgreementAutomationControllerTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly HttpClient authenticatedClient;
    private readonly StatusCache cache;
    private readonly MemoryCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
        Size = 1
    };

    public TransferAgreementAutomationControllerTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        cache = factory.Services.GetService<StatusCache>()!;
        authenticatedClient = factory.CreateAuthenticatedClient(Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task GetStatus_CachedSuccess_ShouldReturnSuccess()
    {
        cache.Cache.Set(CacheValues.Key, CacheValues.Success, cacheOptions);

        var response = await authenticatedClient.GetAsync("transfer-automation/status");
        response.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TransferAutomationStatus>(await response.Content.ReadAsStringAsync());

        status.Should().BeEquivalentTo(new TransferAutomationStatus(CacheValues.Success));
    }

    [Fact]
    public async Task GetStatus_CachedError_ShouldReturnError()
    {
        cache.Cache.Set(CacheValues.Key, CacheValues.Error, cacheOptions);

        var response = await authenticatedClient.GetAsync("transfer-automation/status");
        response.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TransferAutomationStatus>(await response.Content.ReadAsStringAsync());

        status.Should().BeEquivalentTo(new TransferAutomationStatus(CacheValues.Error));
    }

    [Fact]
    public async Task GetStatus_OnEmptyCache_ShouldReturnError_()
    {
        cache.Cache.Clear();

        var response = await authenticatedClient.GetAsync("transfer-automation/status");
        response.EnsureSuccessStatusCode();

        var status = JsonConvert.DeserializeObject<TransferAutomationStatus>(await response.Content.ReadAsStringAsync());

        status.Should().BeEquivalentTo(new TransferAutomationStatus(CacheValues.Error));
    }
}
