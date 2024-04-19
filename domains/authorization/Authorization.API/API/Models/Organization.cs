namespace API.Models;

public class Organization
{
    public Guid Id { get; set; }
    public string IdpId { get; set; }
    public string IdpOrganizationId { get; set; }
    public string TIN { get; set; }
    public string Name { get; set; }
    public ICollection<Affiliation> Affiliations { get; set; }
    public ICollection<Consent> Consents { get; set; }
}
