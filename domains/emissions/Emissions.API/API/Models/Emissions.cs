namespace API.Models
{
    public class Emissions
    {
        public long MeteringPointId { get; set; }
        public long dateFrom { get; set; }
        public long dateTo { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; }
    }
}
