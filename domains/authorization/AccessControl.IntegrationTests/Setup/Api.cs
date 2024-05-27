     using System.Net.Http.Json;

     namespace AccessControl.IntegrationTests.Setup;

    public class Api(HttpClient client) : IAsyncLifetime
    {
        public async Task<HttpResponseMessage> Decision(Guid organizationId)
        {
            return await client.PostAsJsonAsync($"api/access-control/?organizationId={organizationId}", new { });
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            client.Dispose();
            return Task.CompletedTask;
        }
    }
