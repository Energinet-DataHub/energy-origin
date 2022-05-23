namespace API.Helpers
{
    public static class Configuration
    {
        private const string DataSyncEndpoint = "DATASYNCENDPOINT";
        private const string EnergiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";
        private const string DotNetEnvironment = "ASPNETCORE_ENVIRONMENT";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(DataSyncEndpoint) ?? throw new ArgumentNullException();
        }

        public static bool IsDevelopment()
        {
            return "Development".Equals(Environment.GetEnvironmentVariable(DotNetEnvironment), StringComparison.OrdinalIgnoreCase);
        }
    
        public static string GetEnergiDataServiceEndpoint()
        {
            return Environment.GetEnvironmentVariable(EnergiDataServiceEndpoint) ?? throw new ArgumentNullException();
        }
    }
}
