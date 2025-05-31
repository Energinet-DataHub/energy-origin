// using System;
// using System.Collections.Generic;
// using System.Net.Http.Json;
// using System.Text.Json;
// using System.Text.Json.Serialization;
// using System.Threading;
// using System.Threading.Tasks;
// using API.IntegrationTests.Setup.Fixtures;
// using VerifyXunit;
// using Xunit;
//
// namespace API.IntegrationTests.ReportGenerator;
//
// [Collection(PdfTestCollection.CollectionName)]
// public sealed class ReportGenerationPipelineTests(PdfGenerationFixture fixture, ITestOutputHelper output)
//     : IAsyncLifetime
// {
//     [Theory]
//     [InlineData("da-DK")]
//     [InlineData("en-GB")]
//     public async Task GivenValidDateRangeAndLanguages_WhenReportIsRequested_ThenPdfIsGenerated_And_RequestCanBeFetched_And_ReportCanBeDownloadedSuccessfully(string language)
//     {
//         var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
//         var to   = from.AddYears(1);
//
//         var req   = new { StartDate = from.ToUnixTimeSeconds(), EndDate = to.ToUnixTimeSeconds() };
//         var orgId = fixture._orgId.Value;
//
//         var client = fixture.Factory.CreateB2CAuthenticatedClient(
//             sub: Guid.NewGuid(),
//             orgId: orgId,
//             tin:  "12345678",
//             languageHeader: language);
//
//         var ct = CancellationToken.None;
//
//         using var start = await client.PostAsJsonAsync($"/api/reports?organizationId={orgId}", req, cancellationToken: ct);
//         start.EnsureSuccessStatusCode();
//
//         var responseJson = await start.Content.ReadAsStringAsync(ct);
//         var reportId = JsonDocument.Parse(responseJson).RootElement.GetProperty("reportId").GetGuid();
//
//         output.WriteLine($"Report request initiated with ID: {reportId}");
//
//         const int maxTries = 40;
//         var status = "";
//         for (var i = 0; i < maxTries && status != "Completed"; i++)
//         {
//             await Task.Delay(1_000, ct);
//
//             var statuses = await client.GetFromJsonAsync<GetReportStatusesDto>(
//                 $"/api/reports?organizationId={orgId}",
//                 cancellationToken: ct);
//
//             status = statuses?.Result.Find(r => r.ReportId == reportId)?.Status ?? string.Empty;
//
//             output.WriteLine($"Attempt {i + 1}: Report status is '{status}'");
//         }
//
//         Assert.Equal("Completed", status);
//
//         var pdfBytes = await client.GetByteArrayAsync(
//             $"/api/reports/{reportId}/download?organizationId={orgId}", ct);
//
//         output.WriteLine($"Downloaded PDF size: {pdfBytes.Length} bytes");
//
//         await Verifier
//             .Verify(pdfBytes, extension: "pdf")
//             .UseParameters(language)
//             .UseDirectory("Snapshots");
//     }
//
//     private sealed record ReportStatusDto(
//         [property: JsonPropertyName("id")]
//         Guid   ReportId,
//         [property: JsonPropertyName("status")]
//         string Status);
//
//     private sealed record GetReportStatusesDto(
//         [property: JsonPropertyName("result")]
//         List<ReportStatusDto> Result);
//
//     public ValueTask InitializeAsync() => ValueTask.CompletedTask;
//     public ValueTask DisposeAsync() => ValueTask.CompletedTask;
// }
