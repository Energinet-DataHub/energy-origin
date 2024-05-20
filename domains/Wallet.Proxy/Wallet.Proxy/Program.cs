using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Wallet.Proxy;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(tbc =>
    {
        if (!string.IsNullOrEmpty(tbc.Route.AuthorizationPolicy)) return;

        tbc.AddRequestTransform(async transformContext =>
        {
            var organizationId = transformContext.HttpContext.Request.Query["organizationId"].ToString();
            transformContext.ProxyRequest.Headers.Add("wallet-owner", organizationId);
            await Task.CompletedTask;
        });

        tbc.AddQueryRemoveKey("organizationId");
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationHandler, OrganizationHandler>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_CLIENTCREDENTIALS";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
        
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AzureAuthzPolicy", policy =>
    {
        policy.Requirements.Add(new OrganizationRequirement());
        policy.RequireAuthenticatedUser();
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();

public partial class Program
{
}
