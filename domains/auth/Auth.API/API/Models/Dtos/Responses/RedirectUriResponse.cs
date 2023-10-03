namespace API.Models.Dtos.Responses;

public record RedirectUriResponse
{
    public required string RedirectionUri { get; init; }
}
