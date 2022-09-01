using System.Globalization;

namespace API.Configuration
{
    //TODO: Migrate to IOptions
    public static class Configurations
    {
        private static readonly string dataSyncEndpoint = "DATASYNCENDPOINT";
        private static readonly string energiDataServiceEndpoint = "ENERGIDATASERVICEENDPOINT";
        private static readonly string renewableSources = "RENEWABLESOURCES";
        private static readonly string wasteRenewableShare = "WASTERENEWABLESHARE";

        public static int DecimalPrecision => 5;

        public static string GetDataSyncEndpoint() => Environment.GetEnvironmentVariable(dataSyncEndpoint) ?? throw new ArgumentNullException();

        public static string GetEnergiDataServiceEndpoint() => Environment.GetEnvironmentVariable(energiDataServiceEndpoint) ?? throw new ArgumentNullException();

        public static IList<string> GetRenewableSources() => Environment.GetEnvironmentVariable(renewableSources)?.Split(',') ?? throw new ArgumentNullException();

        public static decimal GetWasteRenewableShare() => Convert.ToDecimal(int.Parse(Environment.GetEnvironmentVariable(wasteRenewableShare) ?? throw new ArgumentNullException(), CultureInfo.InvariantCulture)) / 100;
    }
}
