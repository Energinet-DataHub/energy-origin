namespace API.Models
{
    public class Emissions
    {
        public long DateFrom { get; }

        public long DateTo { get; }

        public Quantity Total { get; }

        public Quantity Relative { get; }

        public Emissions(long dateFrom, long dateTo, Quantity total, Quantity relative)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Total = total;
            Relative = relative;
        }
    }
}
