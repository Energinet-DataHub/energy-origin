using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Dtos.Request;

public class RemoveOrganizationFromWhitelistRequest
{
    [Required]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "TIN must be exactly 8 digits")]
    public string Tin { get; set; } = string.Empty;
}
