using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using EnergyOrigin.TokenValidation.Utilities;

namespace Tests.Factories;

public class MeasurementsWebApplicationFactory : WebApplicationFactory<Program>
{
    private byte[] PrivateKey { get; set; } = RsaKeyGenerator.GenerateTestKey();

    public string DataHubFacadeUrl { get; set; } = "http://someurl.com";

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

        builder.UseSetting("TokenValidation:PublicKey", publicKeyBase64);
        builder.UseSetting("TokenValidation:Issuer", "Issuer");
        builder.UseSetting("TokenValidation:Audience", "Audience");
        builder.UseSetting("DataHubFacade:Url", DataHubFacadeUrl);
        builder.UseSetting("DataSync:Endpoint", "https://example.com");
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("EO_API_VERSION", "20240110");
        return client;
    }

    public IApiVersionDescriptionProvider GetApiVersionDescriptionProvider()
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        return provider;
    }
}
