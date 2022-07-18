namespace API.Models
{
    public class MixRecord
    {
        public decimal ShareTotal { get; }

        public DateTime HourUTC { get; }

        public string Version { get; }

        public string GridArea { get; }

        public string ProductionType { get; }

        public MixRecord(decimal shareTotal, DateTime hourUTC, string version, string gridArea, string productionType)
        {
            ShareTotal = shareTotal;
            HourUTC = hourUTC;
            Version = version;
            GridArea = gridArea;
            ProductionType = productionType.Length switch
            {
                0 => "",
                1 => productionType.ToLowerInvariant(),
                _ => char.ToLowerInvariant(productionType[0]) + productionType[1..]
            };
        }
    }
}
