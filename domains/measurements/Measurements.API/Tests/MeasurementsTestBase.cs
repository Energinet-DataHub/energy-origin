using API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using API.MeteringPoints.Api;
using FluentAssertions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Fixtures;
using Tests.TestContainers;
using Xunit;

namespace Tests;

public class MeasurementsTestBase : IClassFixture<TestServerFixture<Startup>>, IClassFixture<PostgresContainer>, IClassFixture<RabbitMqContainer>
{
    protected readonly TestServerFixture<Startup> _serverFixture;
    public string DataHubFacadeUrl { get; set; } = "http://someurl.com";
    public string otlpEndpoint { get; set; } = "http://someurl";


    public MeasurementsTestBase(TestServerFixture<Startup> serverFixture, PostgresContainer dbContainer, RabbitMqContainer rabbitMqContainer, Dictionary<string, string?>? options)
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
            { "RabbitMq:Host", rabbitMqContainer.Options.Host },
            { "RabbitMq:Port", rabbitMqContainer.Options.Port.ToString() },
            { "RabbitMq:Username", rabbitMqContainer.Options.Username },
            { "RabbitMq:Password", rabbitMqContainer.Options.Password },
            { "ConnectionStrings:Postgres", dbContainer.ConnectionString }
        };

        options?.ToList().ForEach(x => config[x.Key] = x.Value);

        _serverFixture.ConfigureHostConfiguration(config);
    }
}
