using System;
using System.Collections.Generic;
using System.Linq;
using AccessControl.API.Configuration;
using AccessControl.API.Swagger;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.AddSerilog();

builder.Services.AddOpenTelemetryMetricsAndTracing("AccessControl.API", otlpOptions.ReceiverEndpoint);

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

// var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
//     builder.Configuration["AzureAdB2C:WellKnownUrl"],
//     new OpenIdConnectConfigurationRetriever(),
//     new HttpDocumentRetriever());

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.Authority = builder.Configuration["AzureAdB2C:Authority"];
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidIssuer = builder.Configuration["AzureAdB2C:Issuer"],
//             ValidateAudience = true,
//             ValidAudiences = new[] { builder.Configuration["AzureAdB2C:Audience"] },
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
//             {
//                 var configuration = configurationManager.GetConfigurationAsync().Result;
//                 return configuration.SigningKeys;
//             }
//         };
//     });

var tokenValidationOptions =
    builder.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
builder.Services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix)
    .ValidateDataAnnotations().ValidateOnStart();
var b2COptions = builder.Configuration.GetSection(B2COptions.Prefix).Get<B2COptions>()!;
builder.Services.AddOptions<B2COptions>().BindConfiguration(B2COptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddB2CAndTokenValidation(b2COptions, tokenValidationOptions);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("OrganizationAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("org_ids");

        policy.RequireAssertion(context =>
        {
            if (context.Resource is not HttpContext httpContext ||
                !Guid.TryParse(httpContext.Request.Query["organizationId"], out Guid organizationId)) return false;

            var orgIdsClaim = context.User.Claims.FirstOrDefault(c => c.Type == "org_ids")?.Value;
            var orgIds = orgIdsClaim != null ? orgIdsClaim.Split(' ').Select(Guid.Parse).ToList() : new List<Guid>();

            return orgIds.Contains(organizationId);
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddVersioningToApi();
builder.Services.AddSwagger("access-control");
builder.Services.AddSwaggerGen(c =>
    c.DocumentFilter<ProblemDetailsDocumentFilter>());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.AddSwagger("access-control");

app.MapControllers();

app.Run();

public partial class Program { }
