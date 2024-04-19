namespace API.Models;

public class Client
{
    public Guid Id { get; set; }
    public string IdpClientId { get; set; }
    public string Name { get; set; }
    public ClientRole Role { get; set; }
    public ICollection<Consent> Consents { get; set; }
}
