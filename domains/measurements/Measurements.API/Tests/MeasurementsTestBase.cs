using API;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using API.MeteringPoints.Api;
using Tests.Fixtures;
using Tests.TestContainers;
using Xunit;

namespace Tests;

public class MeasurementsTestBase : IClassFixture<TestServerFixture<Startup>>
{
    protected readonly TestServerFixture<Startup> _serverFixture;

    public string DataHubFacadeUrl { get; set; } = "http://someurl.com";
    public string otlpEndpoint { get; set; } = "http://someurl";


    public MeasurementsTestBase(TestServerFixture<Startup> serverFixture, string connectionString = "")
    {
        _serverFixture = serverFixture;

        var privateKeyPem = Encoding.UTF8.GetString(serverFixture.PrivateKey);
        string publicKeyPem;

        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKeyPem);
            publicKeyPem = rsa.ExportRSAPublicKeyPem();
        }

        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

        var config = new Dictionary<string, string?>()
        {
            { "Otlp:ReceiverEndpoint", otlpEndpoint },
            { "TokenValidation:PublicKey", publicKeyBase64 },
            { "TokenValidation:Issuer", "demo.energioprindelse.dk" },
            { "TokenValidation:Audience", "Users" },
            { "DataHubFacade:Url", DataHubFacadeUrl },
            { "DataSync:Endpoint", "https://example.com" },
            { "RabbitMq:Host", "localhost" },
            { "RabbitMq:Port", "5672" },
            { "RabbitMq:Username", "guest" },
            { "RabbitMq:Password", "guest" }
        };

        if (!string.IsNullOrEmpty(connectionString))
            config.Add("ConnectionStrings:Postgres", connectionString);

        _serverFixture.ConfigureHostConfiguration(config);
    }
}
