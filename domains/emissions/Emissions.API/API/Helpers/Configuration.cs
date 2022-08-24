namespace API.Helpers
{
    public static class Configuration
    {
        private const string dataSyncEndpoint = "DATASYNCENDPOINT";
        private const string energiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";
        private const string renewableSources = "RENEWABLESOURCES";
        private const string wasteRenewableShare = "WASTERENEWABLESHARE";

        public static int DecimalPrecision => 5;

        public static string GetDataSyncEndpoint()
        {
            return Environment.GetEnvironmentVariable(dataSyncEndpoint) ?? throw new ArgumentNullException();
        }

        public static string GetEnergiDataServiceEndpoint()
        {
            return Environment.GetEnvironmentVariable(energiDataServiceEndpoint) ?? throw new ArgumentNullException();
        }

        public static IList<string> GetRenewableSources()
        {
            var renewableSources = Environment.GetEnvironmentVariable(renewableSources) ?? throw new ArgumentNullException();
            return renewableSources.Split(',');
        }

        public static decimal GetWasteRenewableShare()
        {
            var wasteRenewableShare = Environment.GetEnvironmentVariable(wasteRenewableShare) ?? throw new ArgumentNullException();
            return decimal.Parse(wasteRenewableShare) / 100;
        }
    }
}
