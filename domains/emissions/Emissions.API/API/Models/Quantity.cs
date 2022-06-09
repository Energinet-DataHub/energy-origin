namespace API.Models
{
    public class Quantity : IEquatable<Quantity?>
    {
        public QuantityUnit Unit { get; set; }
        public float Value { get; set; }

        public Quantity(float value, QuantityUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Quantity);
        }

        public bool Equals(Quantity? other)
        {
            return other is not null &&
                   Unit == other.Unit &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Unit, Value);
        }
    }
}
