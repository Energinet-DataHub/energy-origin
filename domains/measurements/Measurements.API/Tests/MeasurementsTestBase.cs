using API;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Contracts;
using Tests.Fixtures;
using Xunit;

namespace Tests;

public class MeasurementsTestBase : IClassFixture<TestServerFixture<Program>>
{
    protected readonly TestServerFixture<Program> _serverFixture;
    public string DataHubFacadeUrl { get; set; } = "http://someurl.com";
    public string otlpEndpoint { get; set; } = "http://someurl";


    public MeasurementsTestBase(TestServerFixture<Program> serverFixture, RabbitMqOptions? rabbitMqOptions = null, string connectionString = "")
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
            { "B2C:B2CWellKnownUrl", "https://login.microsoftonline.com/d3803538-de83-47f3-bc72-54843a8592f2/v2.0/.well-known/openid-configuration" },
            {
                "B2C:ClientCredentialsCustomPolicyWellKnownUrl",
                "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_CLIENTCREDENTIALS"
            },
            {
                "B2C:MitIDCustomPolicyWellKnownUrl",
                "https://datahubeouenerginet.b2clogin.com/datahubeouenerginet.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_MITID"
            },
            { "B2C:Audience", "f00b9b4d-3c59-4c40-b209-2ef87e509f54" },
            { "B2C:CustomPolicyClientId", "a701d13c-2570-46fa-9aa2-8d81f0d8d60b" },
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
        if (rabbitMqOptions != null)
        {
            config["RabbitMq:Host"] = rabbitMqOptions.Host;
            config["RabbitMq:Port"] = rabbitMqOptions.Port.ToString();
            config["RabbitMq:Username"] = rabbitMqOptions.Username;
            config["RabbitMq:Password"] = rabbitMqOptions.Password;
        }

        if (!string.IsNullOrEmpty(connectionString))
            config.Add("ConnectionStrings:Postgres", connectionString);

        _serverFixture.ConfigureHostConfiguration(config);
    }
}
