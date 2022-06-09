namespace API.Models
{
    public class Record
    {
        public double ShareTotal { get; }

        public DateTime HourUTC { get; }

        public string Version { get; }

        public string PriceArea { get; }

        public string ProductionType { get; }

        public Record(double shareTotal, DateTime hourUTC, string version, string priceArea, string productionType)
        {
            ShareTotal = shareTotal;
            HourUTC = hourUTC;
            Version = version;
            PriceArea = priceArea;
            ProductionType = productionType.Length switch
            {
                0 => "",
                1 => productionType.ToLowerInvariant(),
                _ => char.ToLowerInvariant(productionType[0]) + productionType[1..]
            };
        }
    }
}
