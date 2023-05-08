namespace API.Models.Entities;

public record User
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = null!;
    public int AcceptedTermsVersion { get; set; }
    public bool AllowCprLookup { get; set; }

    public Guid? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    public virtual List<UserProvider> UserProviders { get; set; } = new List<UserProvider>();
}
