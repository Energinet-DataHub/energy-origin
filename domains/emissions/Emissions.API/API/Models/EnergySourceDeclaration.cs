namespace API.Models
{
    public class EnergySourceDeclaration
    {
        public EnergySourceDeclaration(long dateFrom, long dateTo, float renewable, Dictionary<string, float> source)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Renewable = renewable;
            Source = source;
        }

        public long DateFrom { get; set; }
        public long DateTo { get; set; }
        public float Renewable { get; set; }
        public Dictionary<string, float> Source {get; set; }
    }
}
