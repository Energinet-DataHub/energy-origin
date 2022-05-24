namespace API.Helpers
{
    public static class Configuration
    {
        private const string DataSyncEndpoint = "DATASYNCENDPOINT";
        private const string EnergiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(DataSyncEndpoint) ?? throw new ArgumentNullException();
        }

        public static string GetEnergiDataServiceEndpoint()
        {
            return Environment.GetEnvironmentVariable(EnergiDataServiceEndpoint) ?? throw new ArgumentNullException();
        }
    }
}
