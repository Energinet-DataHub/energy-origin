namespace API.Models.Entities;

public record User
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = null!;
    public string AcceptedPrivacyPolicyVersion { get; init; }
    public bool AllowCprLookup { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
    public List<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
    public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public List<Role> Roles { get; set; } = new List<Role>();
    public List<UserTerms> UserTerms { get; set; } = new List<UserTerms>();
}
