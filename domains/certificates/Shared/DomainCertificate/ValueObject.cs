namespace DomainCertificate
{

    /// <summary>
    /// Taken from Microsoft: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects
    /// </summary>
    public abstract class ValueObject
    {
        protected static bool EqualOperator(ValueObject left, ValueObject right)
        {
#pragma warning disable IDE0041
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
#pragma warning restore IDE0041
            {
                return false;
            }

            return ReferenceEquals(left, right) || left!.Equals(right);
        }

        protected static bool NotEqualOperator(ValueObject left, ValueObject right)
        {
            return !(EqualOperator(left, right));
        }

        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x.GetHashCode())
                .Aggregate((x, y) => x ^ y);
        }
    }
}
