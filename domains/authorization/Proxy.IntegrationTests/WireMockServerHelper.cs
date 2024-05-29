using WireMock.Server;

namespace Proxy.IntegrationTests
{
    public class WireMockServerHelper : IDisposable
    {
        public WireMockServer Server { get; } = WireMockServer.Start(5001);

        public void Dispose()
        {
            Server.Stop();
            Server.Dispose();
        }
    }
}
