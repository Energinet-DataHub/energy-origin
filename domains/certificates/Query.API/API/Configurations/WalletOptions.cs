using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace API.Configurations;

public class WalletOptions
{
    public const string Wallet = nameof(Wallet);

    [Required]
    public string Url { get; set; } = "";
}

public static partial class OptionsExtensions
{
    public static void AddWalletOptions(this IServiceCollection services) =>
        services.AddOptions<WalletOptions>()
            .BindConfiguration(WalletOptions.Wallet)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
