namespace AdminPortal.Options;

public class CvrOptions
{
    public const string Prefix = "Cvr";

    public bool EnforceCvrValidation { get; set; } = true;

    public string CvrEndpointBasePath { get; set; } = "ett-admin-portal";
}
