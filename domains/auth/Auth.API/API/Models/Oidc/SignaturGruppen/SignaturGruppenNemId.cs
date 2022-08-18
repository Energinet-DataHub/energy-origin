using System.Text.Json.Serialization;

namespace API.Models;

public record SignaturGruppenNemId
{
    public string Iat { get; init; }
    public string Exp { get; init; }
    public string Sub { get; init; }
    public string Idp { get; init; }
    public string IdentityType { get; init; }
    public string Cpr { get; init; }
    public string Cvr { get; init; }

}
