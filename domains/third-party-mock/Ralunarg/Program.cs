using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ralunarg.BackgroundServices;
using Ralunarg.Components;
using Ralunarg.HttpClients;
using Ralunarg.Options;
using System;

namespace Ralunarg
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddOptions<AuthTenantOptions>().BindConfiguration(AuthTenantOptions.AuthTenant).ValidateDataAnnotations().ValidateOnStart();

            builder.Services.AddHostedService<SampledBackgroundService>();
            builder.Services.AddHttpClient<TokenClient>((sp, c) =>
            {
                c.BaseAddress = new Uri("https://login.microsoftonline.com/");
            });
            builder.Services.AddHttpClient<TeamsClient>((sp, c) =>
            {
                c.BaseAddress = new Uri("https://eo-alerts-apim.azure-api.net/");
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
