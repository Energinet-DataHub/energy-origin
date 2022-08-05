using System.Text.Json.Serialization;

namespace API.Models
{
    public class MixRecord
    {
        public DateTime HourUTC { get; }
        [JsonPropertyName("PriceArea")]
        public string GridArea { get; }
        public string Version { get; }
        public decimal ShareTotal { get; }
        public string ProductionType { get; }

        public MixRecord(decimal shareTotal, DateTime hourUTC, string version, string gridArea, string productionType)
        {
            HourUTC = hourUTC;
            GridArea = gridArea;
            Version = version;
            ShareTotal = shareTotal;
            ProductionType = productionType.Length switch
            {
                0 => "",
                1 => productionType.ToLowerInvariant(),
                _ => char.ToLowerInvariant(productionType[0]) + productionType[1..]
            };
        }
    }
}
