namespace API.Models
{
    public class Quantity
    {
        public QuantityUnit Unit { get; set; }
        public decimal Value { get; set; }

        public Quantity(decimal value, QuantityUnit unit)
        {
            Value = value;
            Unit = unit;
        }
    }
}
