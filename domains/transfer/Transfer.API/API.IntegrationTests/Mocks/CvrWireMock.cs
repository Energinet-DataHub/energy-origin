using System;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace API.IntegrationTests.Mocks
{
    public class CvrWireMock : IDisposable
    {
        private readonly WireMockServer server;
        public CvrWireMock() => server = WireMockServer.Start();
        public string Url => server.Url!;

        public void SetupCvrResponse()
        {
            server.ResetMappings();
            server
                .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBodyFromFile("Mocks/cvr_response.json")
                );
        }
        public void SetupEmptyCvrResponse()
        {
            server.ResetMappings();
            server
                .Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBodyFromFile("Mocks/empty_cvr_response.json")
                );
        }

        public void SetupUnstableServer()
        {
            server.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
                .InScenario("UnstableServer")
                .WillSetStateTo("FirstCallDone")
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.InternalServerError));

            server.Given(Request.Create().WithPath("/cvr-permanent/virksomhed/_search").UsingPost())
                .InScenario("UnstableServer")
                .WhenStateIs("FirstCallDone")
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyFromFile("Mocks/cvr_response.json"));
        }

        public void Dispose() => server.Dispose();
    }
}
