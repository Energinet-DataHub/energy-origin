using System.Reflection;
using System.Text;
using System.Text.Json;
using IdentityModel.Client;
using IdentityModel.Jwk;

namespace Unit.Tests;

public static class DiscoveryDocument
{
    public static DiscoveryDocumentResponse Load(IEnumerable<KeyValuePair<string, string>> items,
        JsonWebKeySet? keySet = null)
    {
        var builder = new StringBuilder();
        builder = builder.Append("{ ");
        builder = items.Aggregate(builder, (current, item) => current.Append($""" "{item.Key}":"{item.Value}","""));
        builder.Length--;
        builder = builder.Append('}');

        var json = builder.ToString();

        var element = JsonDocument.Parse(json).RootElement;

        var document = new DiscoveryDocumentResponse();
        var reflection = document.GetType()
            .GetProperty(nameof(document.Json), BindingFlags.Public | BindingFlags.Instance);
        reflection!.SetValue(document, element);

        document.KeySet = keySet;

        return document;
    }
}
