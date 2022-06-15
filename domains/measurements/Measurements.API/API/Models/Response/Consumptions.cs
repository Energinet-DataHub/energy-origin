namespace API.Models
{
    public class Consumptions
    {
        public long DateFrom { get; }

        public long DateTo { get; }

        public float Value { get; }

        public Consumptions(long dateFrom, long dateTo, float value)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Value = value;
        }
    }
}
