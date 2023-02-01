namespace API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string ProviderId { get; set; }
        public string Name { get; set; }
        public string AcceptedTermsVersion { get; set; }
        public string? Tin { get; set; }
        public bool AllowCPRLookup { get; set; }
    }
}
