using WireMock.Server;

namespace Proxy.IntegrationTests.Setup
{
    public class ProxyWireMockServerHelper : IDisposable
    {
        public WireMockServer Server { get; } = WireMockServer.Start(5001);

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
