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

        public long DateFrom { get; }
        public long DateTo { get; }
        public float Renewable { get; }
        public Dictionary<string, float> Ratios { get; }
    }
}
