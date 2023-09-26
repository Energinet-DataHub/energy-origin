using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class EnergiDataServiceOptions
{
    public const string Prefix = "EnergiDataServiceOptions";

    [Required]
    [RegularExpression(@"^https?:\/\/[^\s]+")]
    public Uri Endpoint { get; init; } = null!;

    [Required]
    public string RenewableSourceList { get; init; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public decimal WasteRenewableShare { get; init; } = 0;

    public IEnumerable<string> RenewableSources { get { return RenewableSourceList.Split(","); } }
}
