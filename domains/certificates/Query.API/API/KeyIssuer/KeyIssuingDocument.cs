namespace API.KeyIssuer;

public class KeyIssuingDocument
{
    public string MeteringPointOwner { get; set; } = "";
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string Signature { get; set; } = "";
}
