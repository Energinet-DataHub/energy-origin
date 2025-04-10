using System.ComponentModel.DataAnnotations;

namespace WalletClient;

public class WalletOptions
{
    public const string Wallet = nameof(Wallet);

    [Required]
    public string Url { get; set; } = "";
}

