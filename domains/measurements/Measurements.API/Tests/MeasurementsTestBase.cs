using API;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Tests.Fixtures;
using Xunit;

namespace Tests;

public class MeasurementsTestBase : IClassFixture<TestServerFixture<Startup>>, IDisposable
{
    protected readonly TestServerFixture<Startup> _serverFixture;
    public string DataHubFacadeUrl { get; set; } = "http://someurl.com";

    private bool _disposed = false;

    public MeasurementsTestBase(TestServerFixture<Startup> serverFixture)
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
            {"TokenValidation:PublicKey", publicKeyBase64},
            {"TokenValidation:Issuer", "Issuer"},
            {"TokenValidation:Audience", "Audience"},
            {"DataHubFacade:Url", DataHubFacadeUrl},
            {"DataSync:Endpoint", "https://example.com"}
        };

        _serverFixture.ConfigureHostConfiguration(config);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MeasurementsTestBase()
    {
        Dispose(false);
    }
}
