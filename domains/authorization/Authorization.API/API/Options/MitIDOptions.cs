using System;
using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class MitIDOptions
{
    public const string Prefix = "MitID";

    [Required] public Uri URI { get; set; } = null!;
}
