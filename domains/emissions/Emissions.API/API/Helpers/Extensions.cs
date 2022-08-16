namespace API.Helpers
{
    public static class EnumerableUtils
    {
        public static ulong Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, ulong> selector)
        {
            if (source == null) throw new ArgumentNullException("source");
            ulong sum = 0;
            checked
            {
                foreach (ulong v in Enumerable.Select(source, selector)) sum += v;
            }
            return sum;
        }
    }
}
