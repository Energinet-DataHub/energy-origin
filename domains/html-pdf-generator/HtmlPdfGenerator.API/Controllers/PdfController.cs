using System.Threading.Tasks;
using HtmlPdfGenerator.API.Models;
using HtmlPdfGenerator.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HtmlPdfGenerator.API.Controllers;

[ApiController]
[Route("generate-pdf")]
public class PdfController : ControllerBase
{
    private readonly IPdfRenderer _pdfRenderer;

    public PdfController(IPdfRenderer pdfRenderer)
    {
        _pdfRenderer = pdfRenderer;
    }

    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] HtmlInput input)
    {
        var pdf = await _pdfRenderer.RenderPdfAsync(input.Html);
        return File(pdf, "application/pdf");
    }
}
