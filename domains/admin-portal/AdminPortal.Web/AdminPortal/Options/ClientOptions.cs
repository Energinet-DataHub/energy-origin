using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Options;

public class ClientUriOptions
{
    public const string Prefix = "Endpoint";


    public required string Authorization { get; set; }

    public required string Certificates { get; set; }
}
