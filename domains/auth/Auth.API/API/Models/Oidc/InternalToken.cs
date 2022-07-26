namespace API.Models.Oidc;
public class InternalToken
{
    public DateTime Issued { get; set; }
    public DateTime Expires { get; set; }
    public string Actor { get; set; }
    public string Subject { get; set; }
    public List<string> Scope { get; set; }
}
