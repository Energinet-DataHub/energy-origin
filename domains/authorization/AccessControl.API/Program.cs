using System;
using System.Linq;
using AccessControl.API.Policies;
using AccessControl.API.Swagger;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    builder.Configuration["AzureAdB2C:WellKnownUrl"],
    new OpenIdConnectConfigurationRetriever(),
    new HttpDocumentRetriever());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AzureAdB2C:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AzureAdB2C:Issuer"],
            ValidateAudience = true,
            ValidAudiences = new[] { builder.Configuration["AzureAdB2C:Audience"] },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var configuration = configurationManager.GetConfigurationAsync().Result;
                return configuration.SigningKeys;
            }
        };
    });

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
            var orgIds = orgIdsClaim?.Split(' ') ?? [];

            return orgIds.Contains(organizationId.ToString());
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
