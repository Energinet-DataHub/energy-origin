namespace API.Models
{
    public class EnergySourceDeclaration
    {
        public long DateFrom { get; }
        public long DateTo { get; }
        public decimal Renewable { get; }
        public Dictionary<string, decimal> Ratios { get; }

        public EnergySourceDeclaration(long dateFrom, long dateTo, decimal renewable, Dictionary<string, decimal> ratios)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Renewable = renewable;
            Ratios = ratios;
        }
    }
}
