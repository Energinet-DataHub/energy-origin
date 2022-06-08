namespace API.Models
{
    public class Quantity
    {
        public QuantityUnit Unit { get; set; }
        public float Value { get; set; }

        public Quantity(float value, QuantityUnit unit)
        {
            Value = value;
            Unit = unit;
        }
    }
}
