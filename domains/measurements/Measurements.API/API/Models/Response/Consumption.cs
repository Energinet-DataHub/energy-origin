namespace API.Models
{
    public class Consumption
    {
        public long DateFrom { get; }

        public long DateTo { get; }

        public int Value { get; }

        public Consumption(long dateFrom, long dateTo, int value)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Value = value;
        }
    }
}
