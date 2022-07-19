namespace Oidc.Mock.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> Plus<TKey, TValue>(this Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue>? dict2) where TKey : notnull
    {
        if (dict2 == null)
            return dict1;

        var sameKeys = dict1.Keys.Intersect(dict2.Keys).ToArray();

        if (sameKeys.Any())
        {
            throw new ArgumentException($"Cannot add two dictionaries with same keys: {string.Join(",", sameKeys)}");
        }

        return dict1.Concat(dict2).ToDictionary(x => x.Key, x => x.Value);
    }
}
