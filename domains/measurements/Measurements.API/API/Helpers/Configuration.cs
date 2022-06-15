namespace API.Helpers
{
    public static class Configuration
    {
        private const string DataSyncEndpoint = "DATASYNCENDPOINT";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(DataSyncEndpoint) ?? throw new ArgumentNullException();
        }
    }
}
