using System.Reflection;
using System.Text.Json;
using IdentityModel.Client;

namespace Tests;

public static class DiscoveryDocument
{
    public static DiscoveryDocumentResponse Load(string json)
    {
        var element = JsonDocument.Parse(json)!.RootElement;

        var document = new DiscoveryDocumentResponse();
        var reflection = document.GetType().GetProperty(nameof(document.Json), BindingFlags.Public | BindingFlags.Instance);
        reflection!.SetValue(document, element);

        return document;
    }
}
