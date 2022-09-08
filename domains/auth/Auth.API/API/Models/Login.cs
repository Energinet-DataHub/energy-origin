namespace API.Models;
#nullable disable
public record Login
{
    public string FeUrl { get; init; }
    public string ReturnUrl { get; init; }
}
