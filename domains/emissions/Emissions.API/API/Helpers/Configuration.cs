namespace API.Helpers
{
    public static class Configuration
    {
        private const string DataSyncEndpoint = "DATASYNCENDPOINT";
        private const string EnergiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";
        private const string RenewableSources = "RENEWABLESOURCES";
        private const string WasteRenewableShare = "WASTERENEWABLESHARE";

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
            var renewableSources = Environment.GetEnvironmentVariable(RenewableSources) ?? throw new ArgumentNullException();
            return renewableSources.Split(',');
        }
        public static float GetWasteRenewableShare()
        {
            var wasteRenewableShare = Environment.GetEnvironmentVariable(WasteRenewableShare) ?? throw new ArgumentNullException();
            return float.Parse(wasteRenewableShare) / 100;
        }
    }
}
