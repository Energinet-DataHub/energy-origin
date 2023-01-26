using System.Web;

namespace API.Utilities
{
    public static class UriExtension
    {
        public static Uri AddQueryParameters(this Uri uri, params string[] items)
        {
            var builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query);

            for (var i = 0; i < items.Length - 1; i += 2)
            {
                query[items[i]] = items[i + 1];
            }

            builder.Query = query.ToString();
            return builder.Uri;
        }
    }
}
