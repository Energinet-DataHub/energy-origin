namespace API.Models
{
    public class Quantity
    {
        public QuantityUnit Unit { get; set; }
        public float Value { get; set; }

        public Quantity(float value, QuantityUnit unit)
        {
            Unit = unit;
            Value = value;
        }

        #region Equality
        protected bool Equals(Quantity? other)
        {
            return other != null && Unit == other.Unit && Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
         
            return Equals(obj as Quantity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return HashCode.Combine(Unit, Value);
            }
        }
        #endregion Equality
    }
}
