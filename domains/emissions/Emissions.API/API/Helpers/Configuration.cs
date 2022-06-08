namespace API.Helpers
{
    public static class Configuration
    {
        const string DataSyncEndpoint = "DATASYNCENDPOINT";
        const string EnergiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";
        const string RenewableSources = "RENEWABLESOURCES";
        const string WasteRenewableShare = "WASTERENEWABLESHARE";

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(DataSyncEndpoint) ?? throw new ArgumentNullException();
        }

        public static string GetEnergiDataServiceEndpoint()
        {
            return Environment.GetEnvironmentVariable(EnergiDataServiceEndpoint) ?? throw new ArgumentNullException();
        }

        public static IList<string> GetRenewableSources()
        {
            var renewableSourcesConfiguration = Environment.GetEnvironmentVariable(RenewableSources) ?? throw new ArgumentNullException();
            return renewableSourcesConfiguration.Split(',');
        }
        public static float GetWasteRenewableShare()
        {
            var wasteRenewableShare = Environment.GetEnvironmentVariable(WasteRenewableShare) ?? throw new ArgumentNullException();
            return float.Parse(wasteRenewableShare)/100;
        }
    }
}
