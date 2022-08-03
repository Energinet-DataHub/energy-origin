namespace API.Helpers
{
    public static class Configuration
    {
        const string DataSyncEndpoint = "DATASYNCENDPOINT";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(DataSyncEndpoint) ?? throw new ArgumentNullException();
        }
    }
}
