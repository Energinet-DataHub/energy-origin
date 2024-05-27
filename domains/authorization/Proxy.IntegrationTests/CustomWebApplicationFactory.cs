using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Proxy.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAuthenticationSchemeProvider));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => {});
        });
    }

    public HttpClient CreateAuthenticatedClient(string sub = "", string name = "", List<string>? orgIds = null, string subType = "")
    {
        sub = string.IsNullOrEmpty(sub) ? Guid.NewGuid().ToString() : sub;
        name = string.IsNullOrEmpty(name) ? "Test Testesen" : name;
        subType = string.IsNullOrEmpty(subType) ? "user" : subType;
        orgIds = orgIds ?? new List<string> { Guid.NewGuid().ToString() };

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

        var orgIdsString = string.Join(" ", orgIds ?? new List<string>());

        var identity = new ClaimsIdentity(new List<Claim>
        {
            new("sub", sub),
            new("name", name),
            new("org_ids", orgIdsString),
            new("sub_type", subType),
        });

        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = "f00b9b4d-3c59-4c40-b209-2ef87e509f54",
            Issuer = "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0",
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
