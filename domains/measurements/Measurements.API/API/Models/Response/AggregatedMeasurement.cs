namespace API.Models
{
    public class AggregatedMeasurement
    {
        public long DateFrom { get; }

        public long DateTo { get; }

        public int Value { get; }

        public AggregatedMeasurement(long dateFrom, long dateTo, int value)
        {
            DateFrom = dateFrom;
            DateTo = dateTo;
            Value = value;
        }
    }
}
