using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmlPdfGenerator.IntegrationTests;

public class HealthCheckTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task HealthCheck_ShouldReturnHealthy()
  {
    var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);
    var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    response.EnsureSuccessStatusCode();
    Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
  }
}
