namespace API.Models;
#nullable disable
public record JwtToken
{
    public string Actor { get; init; }
    public string Subject { get; init; }
}
