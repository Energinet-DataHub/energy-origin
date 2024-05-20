using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Wallet.Proxy.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

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

        builder.ConfigureServices(services =>
        {
            builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
            builder.UseSetting("TokenValidation:Issuer", "demo.energioprindelse.dk");
            builder.UseSetting("TokenValidation:Audience", "Users");

            services.AddSingleton(new TokenSigner(PrivateKey));
        });
    }

    public HttpClient CreateAuthenticatedClient(string sub = "", string name = "", List<string>? orgIds = null, string subType = "")
    {
        sub = string.IsNullOrEmpty(sub) ? Guid.NewGuid().ToString() : sub;
        name = string.IsNullOrEmpty(name) ? "Test Testesen" : name;
        subType = string.IsNullOrEmpty(subType) ? "user" : subType;

        var client = CreateClient();
        var token = GenerateToken(sub, name, orgIds, subType);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    
    private string GenerateToken(string sub, string name = "Default Name", List<string>? orgIds = null, string subType = "Default SubType")
    {
        using RSA rsa = RSA.Create(2048);
        var req = new CertificateRequest("cn=eotest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        var signingCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256);
        var tokenHandler = new JwtSecurityTokenHandler();
    
        var orgIdsJson = JsonSerializer.Serialize(orgIds ?? []);

        var identity = new ClaimsIdentity(new List<Claim>
        {
            new("sub", sub),
            new("name", name),
            new("org_ids", orgIdsJson),
            new("sub_type", subType),
        });

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "audience",
            Issuer = "issuer",
            NotBefore = DateTime.Now,
            Expires = DateTime.Now.AddHours(1),
            SigningCredentials = signingCredentials,
            Subject = identity
        };

        var token = tokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = tokenHandler.WriteToken(token);
        return encodedAccessToken;
    }
}