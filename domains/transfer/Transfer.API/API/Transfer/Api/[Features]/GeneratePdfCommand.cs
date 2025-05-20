using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.Setup.Pdf;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace API.Transfer.Api._Features_;

public class GeneratePdfCommandHandler(HttpClient httpClient, IOptions<PdfOptions> pdfOptions)
    : IRequestHandler<GeneratePdfCommand, GeneratePdfResult>
{
    private readonly PdfOptions _pdfOptions = pdfOptions.Value;

    public async Task<GeneratePdfResult> Handle(GeneratePdfCommand request, CancellationToken cancellationToken)
    {
        string html;

        // TODO: MASEP Remove conversion
        try
        {
            html = Encoding.UTF8.GetString(Convert.FromBase64String(request.Base64Html));
        }
        catch (FormatException)
        {
            return new GeneratePdfResult(
                false,
                StatusCode: 400,
                ErrorContent: "The provided HTML must be valid, and base64 encoded.",
                PdfBytes: null);
        }

        if (string.IsNullOrWhiteSpace(html) || !html.TrimStart().StartsWith("<"))
        {
            return new GeneratePdfResult(
                false,
                StatusCode: 400,
                ErrorContent: "The provided HTML must be valid, and base64 encoded.",
                PdfBytes: null);
        }
        var response = await httpClient.PostAsJsonAsync(_pdfOptions.Url, new { Html = html }, cancellationToken);

        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600)
        {
            return new GeneratePdfResult(
                false,
                StatusCode: 400,
                ErrorContent: await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var pdfBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new GeneratePdfResult(true, PdfBytes: pdfBytes);
    }
}

public record GeneratePdfCommand(string Base64Html) : IRequest<GeneratePdfResult>;
public record GeneratePdfResult(bool IsSuccess, byte[]? PdfBytes = null, int? StatusCode = null, string? ErrorContent = null);
