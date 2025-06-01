using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HtmlPdfGenerator.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StartupHealthCheck>();
builder.Services.AddSingleton<IPdfRendererLifecycle, PdfRenderer>();
builder.Services.AddSingleton<IPdfRenderer>(sp => sp.GetRequiredService<IPdfRendererLifecycle>());
builder.Services.AddHostedService<PdfRendererStartup>();
builder.Services.AddHealthChecks().AddCheck<StartupHealthCheck>("startup");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public abstract partial class Program;
