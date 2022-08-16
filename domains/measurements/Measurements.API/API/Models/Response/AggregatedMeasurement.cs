namespace API.Models
{
    public record AggregatedMeasurement
    {
        public long DateFrom { get; init; }

        public long DateTo { get; init; }

        public ulong Value { get; init; }
    }
}
