using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace HtmlPdfGenerator.API.Services;

public interface IPdfRenderer
{
    Task<byte[]> RenderPdfAsync(string html);
}

public interface IPdfRendererLifecycle : IPdfRenderer, IAsyncDisposable
{
  Task InitializeAsync();
}

public class PdfRenderer : IPdfRendererLifecycle
{
    public IBrowser? Browser { get; private set; }
    private IPlaywright? _playwright;
    private readonly SemaphoreSlim _semaphore = new(8);

    public async Task<byte[]> RenderPdfAsync(string html)
    {
        if (Browser == null) throw new InvalidOperationException("Renderer not initialized");

        await _semaphore.WaitAsync();
        try
        {
            var page = await Browser.NewPageAsync();
            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });

            var pdf = await page.PdfAsync(new()
            {
                Format = "A4",
                Margin = new() { Top = "20px", Bottom = "20px" }
            });

            await page.CloseAsync();
            return pdf;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
      if (Browser is not null)
        await Browser.CloseAsync();

      _playwright?.Dispose();
      _semaphore.Dispose();
    }
}

public class PdfRendererStartup(IPdfRendererLifecycle pdfRenderer, StartupHealthCheck healthCheck)
  : IHostedService
{
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await pdfRenderer.InitializeAsync();
    healthCheck.StartupCompleted = true;
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    await pdfRenderer.DisposeAsync();
  }
}

