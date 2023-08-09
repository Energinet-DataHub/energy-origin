using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace RegistryConnector.Worker;

public class ProjectOriginOptions
{
    public const string ProjectOrigin = nameof(ProjectOrigin);

    [Required]
    public string RegistryUrl { get; set; } = "";
    [Required]
    public string RegistryName { get; set; } = "";
    public byte[] Dk1IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();
    public byte[] Dk2IssuerPrivateKeyPem { get; set; } = Array.Empty<byte>();

    [Required]
    public string WalletUrl { get; set; } = "";

    public bool TryGetIssuerKey(string gridArea, out IPrivateKey? issuerKey)
    {
        try
        {
            issuerKey = GetIssuerKey(gridArea);
            return true;
        }
        catch (Exception)
        {
            issuerKey = default;
            return false;
        }
    }

    public IPrivateKey GetIssuerKey(string gridArea)
    {
        if (gridArea.Equals("DK1", StringComparison.OrdinalIgnoreCase))
            return ToPrivateKey(Dk1IssuerPrivateKeyPem);

        if (gridArea.Equals("DK2", StringComparison.OrdinalIgnoreCase))
            return ToPrivateKey(Dk2IssuerPrivateKeyPem);

        throw new ConfigurationException($"Not supported GridArea {gridArea}");
    }

    private static IPrivateKey ToPrivateKey(byte[] key)
        => new Ed25519Algorithm().ImportPrivateKeyText(Encoding.UTF8.GetString(key));
}

public static class OptionsExtensions
{
    public static void AddProjectOriginOptions(this IServiceCollection services) =>
        services.AddOptions<ProjectOriginOptions>()
            .BindConfiguration(ProjectOriginOptions.ProjectOrigin)
            .ValidateDataAnnotations()
            .Validate(o => o.TryGetIssuerKey("DK1", out _), $"Validation failed for '{nameof(ProjectOriginOptions)}' member '{nameof(ProjectOriginOptions.Dk1IssuerPrivateKeyPem)}': Invalid issuer key for DK1")
            .Validate(o => o.TryGetIssuerKey("DK2", out _), $"Validation failed for '{nameof(ProjectOriginOptions)}' member '{nameof(ProjectOriginOptions.Dk2IssuerPrivateKeyPem)}': Invalid issuer key for DK2")
            .ValidateOnStart();
}
