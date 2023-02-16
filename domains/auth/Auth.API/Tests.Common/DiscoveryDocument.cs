using System.Reflection;
using System.Text;
using System.Text.Json;
using IdentityModel.Client;

namespace Tests.Common;

public static class DiscoveryDocument
{
    public static DiscoveryDocumentResponse Load(IEnumerable<KeyValuePair<string, string>> items)
    {
        var builder = new StringBuilder();
        builder = builder.Append("{ ");
        foreach (var item in items)
        {
            builder = builder.Append($""" "{item.Key}":"{item.Value}",""");
        }
        builder.Length--;
        builder = builder.Append('}');

        var json = builder.ToString();

        var element = JsonDocument.Parse(json)!.RootElement;

        var document = new DiscoveryDocumentResponse();
        var reflection = document.GetType().GetProperty(nameof(document.Json), BindingFlags.Public | BindingFlags.Instance);
        reflection!.SetValue(document, element);

        return document;
    }
}
