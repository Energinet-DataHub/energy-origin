namespace API.Models
{
    public class Consumption
    {
        public long DateFrom { get; }

        public long DateTo { get; }

        public float Value { get; }

        public Consumption(long dateFrom, long dateTo, float value)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Value = value;
        }
    }
}
