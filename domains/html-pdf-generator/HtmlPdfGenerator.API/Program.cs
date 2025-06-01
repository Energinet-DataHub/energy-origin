using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HtmlPdfGenerator.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPdfRenderer, PdfRenderer>();
builder.Services.AddHostedService<PdfRendererStartup>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
