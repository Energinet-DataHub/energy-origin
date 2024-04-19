namespace API.Models;

public class User
{
    public Guid Id { get; set; }
    public string IdpId { get; set; }
    public string IdpUserId { get; set; }
    public string Name { get; set; }
    public ICollection<Affiliation> Affiliations { get; set; }
}
