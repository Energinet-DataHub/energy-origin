namespace Domain.Certificates.Primitives
{
    public class Gsrn : ValueObject
    {
        private string value;

        public Gsrn(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("Gsrn cannot be null or whitespaces.");

            this.value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return value;
        }
    }
}
