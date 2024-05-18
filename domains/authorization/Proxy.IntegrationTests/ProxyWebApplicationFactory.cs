using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

namespace Proxy.IntegrationTests;

public class ProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    public string WalletBaseUrl { get; set; } = "SomeUrl";


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var privateKeyPem = Encoding.UTF8.GetString(PrivateKey);
        string publicKeyPem;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKeyPem);
            publicKeyPem = rsa.ExportRSAPublicKeyPem();
        }

        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        builder.ConfigureServices((context, services) =>
        {
            var tokenValidationOptions = context.Configuration.GetSection(TokenValidationOptions.Prefix).Get<TokenValidationOptions>()!;
            services.AddOptions<TokenValidationOptions>().BindConfiguration(TokenValidationOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
            services.AddTokenValidation(tokenValidationOptions);

            builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
            builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
            builder.UseSetting("TokenValidation:Audience", "Users");

            services.AddSingleton(new TokenSigner(PrivateKey));
            services.AddOcelot().AddKubernetes();
        });

        builder.Configure(app =>
        {
            app.UseMiddleware<TokenModificationMiddleware>();
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Request.Path.StartsWithSegments("/wallet-api/v1"))
                {
                    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{ \"sub\": \"{jwtToken.Subject}\" }}");
                }
            });

            app.UseOcelot().Wait();
        });
    }

    public HttpClient CreateAuthenticatedClient(string sub)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(sub));
        return client;
    }

    private string GenerateToken(string sub)
    {
        var tokenSigner = new TokenSigner(PrivateKey);

        var claims = new Dictionary<string, object>
        {
            { UserClaimName.Scope, "" },
            { UserClaimName.ActorLegacy, "d4f32241-442c-4043-8795-a4e6bf574e7f" },
            { UserClaimName.Actor, "d4f32241-442c-4043-8795-a4e6bf574e7f" },
            { UserClaimName.Tin, "11223344" },
            { UserClaimName.OrganizationName, "Producent A/S" },
            { JwtRegisteredClaimNames.Name, "Peter Producent" },
            { UserClaimName.ProviderType, ProviderType.MitIdProfessional.ToString() },
            { UserClaimName.AllowCprLookup, "false" },
            { UserClaimName.AccessToken, "" },
            { UserClaimName.IdentityToken, "" },
            { UserClaimName.ProviderKeys, "" },
            { UserClaimName.OrganizationId, sub },
            { UserClaimName.MatchedRoles, "" },
            { UserClaimName.Roles, "" },
            { UserClaimName.AssignedRoles, "" }
        };

        var issuer = "demo.energioprindelse.dk";
        var audience = "Users";

        return tokenSigner.Sign(
            subject: sub,
            name: "Peter Producent",
            issuer: issuer,
            audience: audience,
            issueAt: DateTime.UtcNow,
            duration: 60,
            claims: claims
        );
    }

    public void Start() => Server.Should().NotBeNull();
}
