namespace API.Helpers
{
    public static class Configuration
    {
        const string DataSyncEndpoint = "DATASYNCENDPOINT";
        const string EnergiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";

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
