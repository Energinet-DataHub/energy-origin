using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Setup.Pdf;
using MediatR;
using Microsoft.Extensions.Options;

namespace API.Transfer.Api._Features_;

public class GeneratePdfCommandHandler(HttpClient httpClient, IOptions<PdfOptions> pdfOptions)
    : IRequestHandler<GeneratePdfCommand, GeneratePdfResult>
{
    private readonly PdfOptions _pdfOptions = pdfOptions.Value;

    public async Task<GeneratePdfResult> Handle(GeneratePdfCommand request, CancellationToken cancellationToken)
    {
        var html = Encoding.UTF8.GetString(Convert.FromBase64String(request.Base64Html));
        var response = await httpClient.PostAsJsonAsync(_pdfOptions.Url, new { Html = html }, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new GeneratePdfResult(true, PdfBytes: pdfBytes);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return new GeneratePdfResult(false, StatusCode: (int)response.StatusCode, ErrorContent: errorContent);
    }
}

public record GeneratePdfCommand(string Base64Html) : IRequest<GeneratePdfResult>;
public record GeneratePdfResult(bool IsSuccess, byte[]? PdfBytes = null, int? StatusCode = null, string? ErrorContent = null);
