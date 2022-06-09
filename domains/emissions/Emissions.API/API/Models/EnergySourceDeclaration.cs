namespace API.Models
{
    public class EnergySourceDeclaration
    {
        public EnergySourceDeclaration(long dateFrom, long dateTo, float renewable, Dictionary<string, float> ratios)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Renewable = renewable;
            Ratios = ratios;
        }

        public long DateFrom { get; set; }
        public long DateTo { get; set; }
        public float Renewable { get; set; }
        public Dictionary<string, float> Ratios {get; set; }
    }
}
