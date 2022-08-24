namespace API.Helpers
{
    public static class Configuration
    {
        const string dataSyncEndpoint = "DATASYNCENDPOINT";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(dataSyncEndpoint) ?? throw new ArgumentNullException();
        }
    }
}
