namespace API.Helpers
{
    public static class Configuration
    {
        private const string dataSyncEndpoint = "DATASYNCENDPOINT";

        public static string GetDataSyncEndpoint() => Environment.GetEnvironmentVariable(dataSyncEndpoint) ?? throw new ArgumentNullException();
    }
}
